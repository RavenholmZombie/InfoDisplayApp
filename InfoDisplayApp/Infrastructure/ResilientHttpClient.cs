using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace InfoDisplayApp.Infrastructure
{
    /// <summary>
    /// Small resilience wrapper used by the weather controls.
    /// Retries transient failures, falls back from Open-Meteo to the
    /// National Weather Service for US coordinates, and finally uses the
    /// last successful cached response instead of blanking the dashboard.
    /// </summary>
    internal sealed class ResilientHttpClient
    {
        private static readonly System.Net.Http.HttpClient _client = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        private static readonly System.Net.Http.HttpClient _nwsClient = CreateNwsClient();
        private static readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
        private static readonly object _diskCacheSync = new();

        private static readonly TimeSpan MaxCachedAge = TimeSpan.FromHours(12);
        private const int MaxAttempts = 4;

        private static string CacheDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "InfoDisplayApp",
            "WeatherCache");

        public async Task<string> GetStringAsync(string requestUri)
        {
            Exception? lastException = null;

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    using HttpResponseMessage response =
                        await _client.GetAsync(requestUri).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        string content =
                            await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        if (!TryValidateJson(content, out JsonException? jsonException))
                        {
                            lastException = new HttpRequestException(
                                "Weather service returned a successful HTTP response containing invalid JSON.",
                                jsonException,
                                response.StatusCode);
                        }
                        else
                        {
                            CacheSuccessfulResponse(requestUri, content);
                            return content;
                        }
                    }
                    else
                    {
                        if (!IsTransient(response.StatusCode))
                            response.EnsureSuccessStatusCode();

                        lastException = new HttpRequestException(
                            $"Weather service returned HTTP {(int)response.StatusCode} ({response.StatusCode}).",
                            null,
                            response.StatusCode);
                    }
                }
                catch (TaskCanceledException ex)
                {
                    lastException = ex;
                }
                catch (HttpRequestException ex)
                {
                    lastException = ex;
                }

                if (attempt < MaxAttempts)
                {
                    int delayMilliseconds = 500 * (1 << (attempt - 1));
                    await Task.Delay(delayMilliseconds).ConfigureAwait(false);
                }
            }

            // Open-Meteo is the primary source. If it remains unavailable after
            // retries, ask api.weather.gov for the same coordinates and convert
            // the NWS forecast into the small Open-Meteo-shaped payload expected
            // by the existing weather widget/ticker parsers.
            if (TryGetOpenMeteoCoordinates(requestUri, out double latitude, out double longitude))
            {
                try
                {
                    string nwsContent = await GetNwsFallbackAsync(
                        requestUri,
                        latitude,
                        longitude).ConfigureAwait(false);

                    if (TryValidateJson(nwsContent, out _))
                    {
                        CacheSuccessfulResponse(requestUri, nwsContent);
                        Debug.WriteLine(
                            $"Open-Meteo was unavailable; using National Weather Service fallback for " +
                            $"{latitude:0.####}, {longitude:0.####}.");
                        return nwsContent;
                    }
                }
                catch (Exception ex) when (
                    ex is HttpRequestException ||
                    ex is TaskCanceledException ||
                    ex is JsonException ||
                    ex is InvalidOperationException)
                {
                    Debug.WriteLine($"NWS weather fallback could not be used: {ex.Message}");
                    lastException = new HttpRequestException(
                        "Both Open-Meteo and the National Weather Service fallback were unavailable.",
                        ex);
                }
            }

            if (TryGetCachedResponse(requestUri, out CacheEntry cached))
            {
                Debug.WriteLine(
                    $"Weather providers had a transient issue; using cached data from " +
                    $"{cached.Timestamp.LocalDateTime:t}.");

                return cached.Content;
            }

            throw lastException ??
                new HttpRequestException("Weather request could not be completed.");
        }

        private static System.Net.Http.HttpClient CreateNwsClient()
        {
            System.Net.Http.HttpClient client = new()
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "InfoDisplayApp/1.0 (+https://github.com/RavenholmZombie/InfoDisplayApp)");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/geo+json");
            return client;
        }

        private static async Task<string> GetNwsFallbackAsync(
            string originalRequestUri,
            double latitude,
            double longitude)
        {
            string coordinateText =
                latitude.ToString("0.####", CultureInfo.InvariantCulture) + "," +
                longitude.ToString("0.####", CultureInfo.InvariantCulture);

            string pointsJson = await GetNwsJsonWithRetryAsync(
                $"https://api.weather.gov/points/{coordinateText}").ConfigureAwait(false);

            using JsonDocument pointsDocument = JsonDocument.Parse(pointsJson);
            JsonElement pointProperties = pointsDocument.RootElement.GetProperty("properties");

            string forecastUrl = pointProperties.GetProperty("forecast").GetString()
                ?? throw new InvalidOperationException("NWS points response did not contain a forecast URL.");
            string hourlyUrl = pointProperties.GetProperty("forecastHourly").GetString()
                ?? throw new InvalidOperationException("NWS points response did not contain an hourly forecast URL.");

            Task<string> forecastTask = GetNwsJsonWithRetryAsync(forecastUrl);
            Task<string> hourlyTask = GetNwsJsonWithRetryAsync(hourlyUrl);
            await Task.WhenAll(forecastTask, hourlyTask).ConfigureAwait(false);

            return BuildOpenMeteoCompatiblePayload(
                originalRequestUri,
                forecastTask.Result,
                hourlyTask.Result);
        }

        private static async Task<string> GetNwsJsonWithRetryAsync(string requestUri)
        {
            Exception? lastException = null;

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    using HttpResponseMessage response =
                        await _nwsClient.GetAsync(requestUri).ConfigureAwait(false);

                    string content =
                        await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (response.IsSuccessStatusCode && TryValidateJson(content, out _))
                        return content;

                    if (!response.IsSuccessStatusCode && !IsTransient(response.StatusCode))
                        response.EnsureSuccessStatusCode();

                    lastException = new HttpRequestException(
                        $"NWS returned HTTP {(int)response.StatusCode} ({response.StatusCode}).",
                        null,
                        response.StatusCode);
                }
                catch (TaskCanceledException ex)
                {
                    lastException = ex;
                }
                catch (HttpRequestException ex)
                {
                    lastException = ex;
                }

                if (attempt < 3)
                    await Task.Delay(750 * attempt).ConfigureAwait(false);
            }

            throw lastException ?? new HttpRequestException("NWS request failed.");
        }

        private static string BuildOpenMeteoCompatiblePayload(
            string originalRequestUri,
            string forecastJson,
            string hourlyJson)
        {
            using JsonDocument forecastDocument = JsonDocument.Parse(forecastJson);
            using JsonDocument hourlyDocument = JsonDocument.Parse(hourlyJson);

            JsonElement forecastPeriods =
                forecastDocument.RootElement.GetProperty("properties").GetProperty("periods");
            JsonElement hourlyPeriods =
                hourlyDocument.RootElement.GetProperty("properties").GetProperty("periods");

            if (forecastPeriods.GetArrayLength() == 0 || hourlyPeriods.GetArrayLength() == 0)
                throw new InvalidOperationException("NWS returned no forecast periods.");

            JsonElement currentPeriod = hourlyPeriods[0];
            int currentTemperature = currentPeriod.GetProperty("temperature").GetInt32();
            string currentCondition = currentPeriod.GetProperty("shortForecast").GetString() ?? "Cloudy";
            bool isDay = currentPeriod.TryGetProperty("isDaytime", out JsonElement currentIsDay) &&
                         currentIsDay.GetBoolean();

            List<NwsPeriod> periods = forecastPeriods
                .EnumerateArray()
                .Select(ParseNwsPeriod)
                .ToList();

            NwsPeriod? today = periods.FirstOrDefault(period => period.IsDaytime);
            NwsPeriod? tonight = periods.FirstOrDefault(period => !period.IsDaytime);

            if (today == null)
                today = periods.FirstOrDefault();
            if (tonight == null)
                tonight = periods.Skip(1).FirstOrDefault() ?? periods.FirstOrDefault();

            List<string> hourlyTimes = new();
            List<int> hourlyCodes = new();

            foreach (JsonElement period in hourlyPeriods.EnumerateArray())
            {
                string? startTime = period.GetProperty("startTime").GetString();
                if (string.IsNullOrWhiteSpace(startTime))
                    continue;

                if (!DateTimeOffset.TryParse(startTime, out DateTimeOffset parsedTime))
                    continue;

                string shortForecast = period.GetProperty("shortForecast").GetString() ?? "Cloudy";
                hourlyTimes.Add(parsedTime.DateTime.ToString("yyyy-MM-dd'T'HH:mm"));
                hourlyCodes.Add(MapNwsConditionToWmo(shortForecast));
            }

            DateTime localToday = DateTime.Now.Date;
            string todayDate = localToday.ToString("yyyy-MM-dd");
            string tomorrowDate = localToday.AddDays(1).ToString("yyyy-MM-dd");

            int todayCode = MapNwsConditionToWmo(today?.ShortForecast ?? currentCondition);
            int tonightCode = MapNwsConditionToWmo(tonight?.ShortForecast ?? currentCondition);
            int todayHigh = today?.Temperature ?? currentTemperature;
            int tonightLow = tonight?.Temperature ?? currentTemperature;

            // The existing ticker reads lows[1] as tonight's low. Duplicate the
            // same NWS nighttime value into both slots so its parser stays valid.
            var payload = new
            {
                current = new
                {
                    temperature_2m = currentTemperature,
                    weather_code = MapNwsConditionToWmo(currentCondition),
                    is_day = isDay ? 1 : 0
                },
                hourly = new
                {
                    time = hourlyTimes,
                    weather_code = hourlyCodes
                },
                daily = new
                {
                    time = new[] { todayDate, tomorrowDate },
                    weather_code = new[] { todayCode, tonightCode },
                    temperature_2m_max = new[] { todayHigh, todayHigh },
                    temperature_2m_min = new[] { tonightLow, tonightLow }
                }
            };

            return JsonSerializer.Serialize(payload);
        }

        private static NwsPeriod ParseNwsPeriod(JsonElement period)
        {
            return new NwsPeriod(
                period.GetProperty("temperature").GetInt32(),
                period.TryGetProperty("isDaytime", out JsonElement isDaytime) && isDaytime.GetBoolean(),
                period.GetProperty("shortForecast").GetString() ?? "Cloudy");
        }

        private static int MapNwsConditionToWmo(string condition)
        {
            string value = condition.ToLowerInvariant();

            if (value.Contains("thunder")) return value.Contains("severe") ? 96 : 95;
            if (value.Contains("freezing rain") || value.Contains("ice")) return 67;
            if (value.Contains("freezing drizzle")) return 57;
            if (value.Contains("snow shower")) return 85;
            if (value.Contains("heavy snow")) return 75;
            if (value.Contains("snow")) return 73;
            if (value.Contains("heavy rain") || value.Contains("heavy shower")) return 82;
            if (value.Contains("showers")) return 81;
            if (value.Contains("rain")) return 63;
            if (value.Contains("drizzle")) return 53;
            if (value.Contains("fog")) return 45;
            if (value.Contains("mostly cloudy") || value.Contains("overcast")) return 3;
            if (value.Contains("partly cloudy") || value.Contains("partly sunny")) return 2;
            if (value.Contains("mostly sunny") || value.Contains("mostly clear")) return 1;
            if (value.Contains("sunny") || value.Contains("clear")) return 0;
            if (value.Contains("cloud")) return 3;

            return 3;
        }

        private static bool TryGetOpenMeteoCoordinates(
            string requestUri,
            out double latitude,
            out double longitude)
        {
            latitude = 0;
            longitude = 0;

            if (!Uri.TryCreate(requestUri, UriKind.Absolute, out Uri? uri) ||
                !uri.Host.Equals("api.open-meteo.com", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            Dictionary<string, string> query = uri.Query
                .TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('=', 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(
                    parts => Uri.UnescapeDataString(parts[0]),
                    parts => Uri.UnescapeDataString(parts[1]),
                    StringComparer.OrdinalIgnoreCase);

            return query.TryGetValue("latitude", out string? latText) &&
                   query.TryGetValue("longitude", out string? lonText) &&
                   double.TryParse(latText, NumberStyles.Float, CultureInfo.InvariantCulture, out latitude) &&
                   double.TryParse(lonText, NumberStyles.Float, CultureInfo.InvariantCulture, out longitude);
        }

        private static void CacheSuccessfulResponse(string requestUri, string content)
        {
            CacheEntry entry = new(content, DateTimeOffset.UtcNow);
            _cache[requestUri] = entry;
            WriteDiskCache(requestUri, entry);
        }

        private static bool TryGetCachedResponse(string requestUri, out CacheEntry cached)
        {
            if (_cache.TryGetValue(requestUri, out cached) &&
                IsUsableCacheEntry(cached))
            {
                return true;
            }

            if (TryReadDiskCache(requestUri, out cached) && IsUsableCacheEntry(cached))
            {
                _cache[requestUri] = cached;
                return true;
            }

            cached = default;
            return false;
        }

        private static bool IsUsableCacheEntry(CacheEntry entry) =>
            DateTimeOffset.UtcNow - entry.Timestamp <= MaxCachedAge &&
            TryValidateJson(entry.Content, out _);

        private static void WriteDiskCache(string requestUri, CacheEntry entry)
        {
            try
            {
                lock (_diskCacheSync)
                {
                    Directory.CreateDirectory(CacheDirectory);
                    string path = GetCachePath(requestUri);
                    File.WriteAllText(path, JsonSerializer.Serialize(entry));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to write weather disk cache: {ex.Message}");
            }
        }

        private static bool TryReadDiskCache(string requestUri, out CacheEntry entry)
        {
            entry = default;

            try
            {
                lock (_diskCacheSync)
                {
                    string path = GetCachePath(requestUri);
                    if (!File.Exists(path))
                        return false;

                    CacheEntry? cached = JsonSerializer.Deserialize<CacheEntry>(File.ReadAllText(path));
                    if (!cached.HasValue)
                        return false;

                    entry = cached.Value;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to read weather disk cache: {ex.Message}");
                return false;
            }
        }

        private static string GetCachePath(string requestUri)
        {
            byte[] bytes = System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(requestUri));
            string fileName = Convert.ToHexString(bytes) + ".json";
            return Path.Combine(CacheDirectory, fileName);
        }

        private static bool TryValidateJson(string content, out JsonException? exception)
        {
            exception = null;

            if (string.IsNullOrWhiteSpace(content))
            {
                exception = new JsonException("Weather service returned an empty response body.");
                return false;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(content);

                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    exception = new JsonException(
                        $"Weather service returned JSON with an unexpected root type: {document.RootElement.ValueKind}.");
                    return false;
                }

                return true;
            }
            catch (JsonException ex)
            {
                exception = ex;
                return false;
            }
        }

        private static bool IsTransient(HttpStatusCode statusCode)
        {
            int code = (int)statusCode;

            return statusCode == HttpStatusCode.RequestTimeout ||
                   code == 429 ||
                   code >= 500;
        }

        private readonly record struct CacheEntry(
            string Content,
            DateTimeOffset Timestamp);

        private sealed record NwsPeriod(
            int Temperature,
            bool IsDaytime,
            string ShortForecast);
    }
}
