using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace InfoDisplayApp.Services
{
    internal sealed class NwsAlertService
    {
        private static readonly (string Name, double Latitude, double Longitude)[] AlertPoints =
        {
            ("Princeton", 45.143109, -67.526589),
            ("Calais", 45.18829, -67.27664)
        };

        // GlobalUsings.cs aliases HttpClient to the weather-specific
        // ResilientHttpClient wrapper. This service needs the real HTTP client
        // because it configures headers, timeout, and reads HttpResponseMessage.
        private readonly System.Net.Http.HttpClient _httpClient;

        public NwsAlertService()
        {
            _httpClient = new System.Net.Http.HttpClient
            {
                Timeout = TimeSpan.FromSeconds(12)
            };

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "InfoDisplayApp/1.0 (+https://github.com/RavenholmZombie/InfoDisplayApp)");
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/geo+json"));
        }

        public async Task<IReadOnlyList<NwsAlertMessage>> GetActiveAlertsAsync(
            CancellationToken cancellationToken = default)
        {
            Dictionary<string, NwsAlertMessage> alerts =
                new(StringComparer.OrdinalIgnoreCase);

            foreach ((string name, double latitude, double longitude) in AlertPoints)
            {
                try
                {
                    string coordinate =
                        latitude.ToString("0.######", CultureInfo.InvariantCulture) + "," +
                        longitude.ToString("0.######", CultureInfo.InvariantCulture);

                    string url =
                        $"https://api.weather.gov/alerts/active?point={coordinate}";

                    using HttpResponseMessage response =
                        await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

                    response.EnsureSuccessStatusCode();

                    string json =
                        await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                    using JsonDocument document = JsonDocument.Parse(json);
                    JsonElement root = document.RootElement;

                    if (!root.TryGetProperty("features", out JsonElement features) ||
                        features.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (JsonElement feature in features.EnumerateArray())
                    {
                        NwsAlertMessage? alert = ParseAlert(feature, name);
                        if (alert == null)
                            continue;

                        alerts.TryAdd(alert.Id, alert);
                    }
                }
                catch (Exception ex) when (
                    ex is HttpRequestException ||
                    ex is TaskCanceledException ||
                    ex is JsonException)
                {
                    Debug.WriteLine($"NWS alert check failed for {name}: {ex.Message}");
                }
            }

            return alerts.Values
                .OrderByDescending(alert => alert.SeverityRank)
                .ThenBy(alert => alert.Sent)
                .ToArray();
        }

        private static NwsAlertMessage? ParseAlert(JsonElement feature, string matchedPoint)
        {
            string id = GetString(feature, "id");
            if (string.IsNullOrWhiteSpace(id) ||
                !feature.TryGetProperty("properties", out JsonElement properties))
            {
                return null;
            }

            string status = GetString(properties, "status");
            string messageType = GetString(properties, "messageType");

            if (!status.Equals("Actual", StringComparison.OrdinalIgnoreCase) ||
                messageType.Equals("Cancel", StringComparison.OrdinalIgnoreCase) ||
                messageType.Equals("Error", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string eventName = GetString(properties, "event");
            string headline = GetString(properties, "headline");
            string description = GetString(properties, "description");
            string instruction = GetString(properties, "instruction");
            string severity = GetString(properties, "severity");
            string urgency = GetString(properties, "urgency");
            string senderName = GetString(properties, "senderName");

            DateTimeOffset? sent = ParseDate(properties, "sent");
            DateTimeOffset? expires = ParseDate(properties, "expires");

            if (expires.HasValue && expires.Value <= DateTimeOffset.UtcNow)
                return null;

            string displayText = BuildDisplayText(
                eventName,
                headline,
                description,
                instruction);

            if (string.IsNullOrWhiteSpace(displayText))
                return null;

            return new NwsAlertMessage(
                id,
                string.IsNullOrWhiteSpace(eventName) ? "Weather Alert" : eventName,
                displayText,
                BuildSpeechText(eventName, description, instruction),
                severity,
                urgency,
                senderName,
                matchedPoint,
                sent,
                expires,
                GetSeverityRank(severity));
        }

        private static string BuildDisplayText(
            string eventName,
            string headline,
            string description,
            string instruction)
        {
            List<string> pieces = new();

            if (!string.IsNullOrWhiteSpace(headline))
                pieces.Add(headline);
            else if (!string.IsNullOrWhiteSpace(eventName))
                pieces.Add(eventName);

            if (!string.IsNullOrWhiteSpace(description))
                pieces.Add(description);

            if (!string.IsNullOrWhiteSpace(instruction))
                pieces.Add("Instructions: " + instruction);

            return NormalizeWhitespace(string.Join("  •  ", pieces));
        }

        private static string BuildSpeechText(
            string eventName,
            string description,
            string instruction)
        {
            List<string> pieces = new();

            if (!string.IsNullOrWhiteSpace(eventName))
                pieces.Add($"The National Weather Service has issued a {eventName} for the Princeton and Calais area.");
            else
                pieces.Add("The National Weather Service has issued an alert for the Princeton and Calais area.");

            if (!string.IsNullOrWhiteSpace(description))
                pieces.Add(description);

            if (!string.IsNullOrWhiteSpace(instruction))
                pieces.Add("Instructions. " + instruction);

            return NormalizeWhitespace(string.Join(" ", pieces));
        }

        private static string NormalizeWhitespace(string value) =>
            Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();

        private static string GetString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement property) ||
                property.ValueKind == JsonValueKind.Null)
            {
                return string.Empty;
            }

            return property.GetString() ?? string.Empty;
        }

        private static DateTimeOffset? ParseDate(JsonElement element, string propertyName)
        {
            string value = GetString(element, propertyName);
            return DateTimeOffset.TryParse(value, out DateTimeOffset parsed)
                ? parsed
                : null;
        }

        private static int GetSeverityRank(string severity) =>
            severity.ToLowerInvariant() switch
            {
                "extreme" => 4,
                "severe" => 3,
                "moderate" => 2,
                "minor" => 1,
                _ => 0
            };
    }

    internal sealed record NwsAlertMessage(
        string Id,
        string EventName,
        string DisplayText,
        string SpeechText,
        string Severity,
        string Urgency,
        string SenderName,
        string MatchedPoint,
        DateTimeOffset? Sent,
        DateTimeOffset? Expires,
        int SeverityRank);
}
