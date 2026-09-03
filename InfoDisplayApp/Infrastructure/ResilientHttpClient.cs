using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace InfoDisplayApp.Infrastructure
{
    /// <summary>
    /// Small resilience wrapper used by the weather controls.
    /// Retries transient failures and falls back to the last known-good
    /// weather response when the upstream service is temporarily unavailable.
    /// </summary>
    internal sealed class ResilientHttpClient
    {
        private static readonly System.Net.Http.HttpClient _client = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        private static readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
        private static readonly TimeSpan MaxCachedAge = TimeSpan.FromHours(12);
        private static readonly string CacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "InfoDisplayApp",
            "WeatherCache");

        private const int MaxAttempts = 4;

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
                            CacheEntry entry = new(content, DateTimeOffset.UtcNow);
                            _cache[requestUri] = entry;
                            await SaveDiskCacheAsync(requestUri, entry).ConfigureAwait(false);
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

            CacheEntry? cached = await GetUsableCacheEntryAsync(requestUri).ConfigureAwait(false);
            if (cached.HasValue)
            {
                Debug.WriteLine(
                    $"Weather service is temporarily unavailable; using cached data from " +
                    $"{cached.Value.Timestamp.LocalDateTime:t}.");

                return cached.Value.Content;
            }

            throw lastException ??
                new HttpRequestException("Weather request could not be completed.");
        }

        private static async Task<CacheEntry?> GetUsableCacheEntryAsync(string requestUri)
        {
            if (_cache.TryGetValue(requestUri, out CacheEntry memoryEntry) &&
                IsUsable(memoryEntry))
            {
                return memoryEntry;
            }

            CacheEntry? diskEntry = await LoadDiskCacheAsync(requestUri).ConfigureAwait(false);
            if (diskEntry.HasValue && IsUsable(diskEntry.Value))
            {
                _cache[requestUri] = diskEntry.Value;
                return diskEntry.Value;
            }

            return null;
        }

        private static bool IsUsable(CacheEntry entry) =>
            DateTimeOffset.UtcNow - entry.Timestamp <= MaxCachedAge &&
            TryValidateJson(entry.Content, out _);

        private static async Task SaveDiskCacheAsync(string requestUri, CacheEntry entry)
        {
            try
            {
                Directory.CreateDirectory(CacheDirectory);
                string path = GetCachePath(requestUri);
                string json = JsonSerializer.Serialize(new DiskCacheEntry(entry.Content, entry.Timestamp));
                await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to save weather cache: {ex.Message}");
            }
        }

        private static async Task<CacheEntry?> LoadDiskCacheAsync(string requestUri)
        {
            try
            {
                string path = GetCachePath(requestUri);
                if (!File.Exists(path))
                    return null;

                string json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
                DiskCacheEntry? diskEntry = JsonSerializer.Deserialize<DiskCacheEntry>(json);

                if (diskEntry == null || string.IsNullOrWhiteSpace(diskEntry.Content))
                    return null;

                return new CacheEntry(diskEntry.Content, diskEntry.Timestamp);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to read weather cache: {ex.Message}");
                return null;
            }
        }

        private static string GetCachePath(string requestUri)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(requestUri));
            string fileName = Convert.ToHexString(hash) + ".json";
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

        private sealed record DiskCacheEntry(
            string Content,
            DateTimeOffset Timestamp);
    }
}
