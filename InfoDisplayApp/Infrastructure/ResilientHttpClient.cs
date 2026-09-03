using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace InfoDisplayApp.Infrastructure
{
    /// <summary>
    /// Small resilience wrapper used by the weather controls.
    /// Retries transient failures and briefly falls back to the last
    /// successful response instead of blanking the dashboard.
    /// </summary>
    internal sealed class ResilientHttpClient
    {
        private static readonly System.Net.Http.HttpClient _client = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        private static readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

        private static readonly TimeSpan MaxCachedAge = TimeSpan.FromHours(2);
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
                            _cache[requestUri] = new CacheEntry(content, DateTimeOffset.UtcNow);
                            return content;
                        }
                    }
                    else
                    {
                        if (!IsTransient(response.StatusCode))
                        {
                            response.EnsureSuccessStatusCode();
                        }

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

            if (_cache.TryGetValue(requestUri, out CacheEntry cached) &&
                DateTimeOffset.UtcNow - cached.Timestamp <= MaxCachedAge &&
                TryValidateJson(cached.Content, out _))
            {
                Debug.WriteLine(
                    $"Weather request had a transient issue; using cached data from " +
                    $"{cached.Timestamp.LocalDateTime:t}.");

                return cached.Content;
            }

            throw lastException ??
                new HttpRequestException("Weather request could not be completed.");
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
    }
}
