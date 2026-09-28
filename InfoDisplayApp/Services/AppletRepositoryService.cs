using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InfoDisplayApp.Services;

public sealed class AppletRepositoryService
{
    private const string RawRoot = "https://raw.githubusercontent.com/RavenholmZombie/InfoScreenAppRepository/main/";
    private const string AppletsApiUrl = "https://api.github.com/repos/RavenholmZombie/InfoScreenAppRepository/contents/applets?ref=main";

    private readonly System.Net.Http.HttpClient _httpClient = CreateHttpClient();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<AppletDefinition>> GetAvailableAppletsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<GitHubContentItem>? files = await _httpClient.GetFromJsonAsync<IReadOnlyList<GitHubContentItem>>(
            AppletsApiUrl, JsonOptions, cancellationToken);

        if (files == null)
            return Array.Empty<AppletDefinition>();

        List<AppletDefinition> result = new();

        foreach (GitHubContentItem file in files.Where(item =>
                     item.Type.Equals("file", StringComparison.OrdinalIgnoreCase) &&
                     item.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                string appletUrl = !string.IsNullOrWhiteSpace(file.DownloadUrl)
                    ? file.DownloadUrl
                    : new Uri(new Uri(RawRoot), "applets/" + file.Name).ToString();

                AppletDefinition? applet = await _httpClient.GetFromJsonAsync<AppletDefinition>(
                    appletUrl, JsonOptions, cancellationToken);

                if (applet != null && AppletManager.IsValid(applet))
                    result.Add(applet);
            }
            catch
            {
                // One malformed/unavailable applet should not prevent the rest
                // of the InfoStore catalog from loading.
            }
        }

        return result
            .GroupBy(a => a.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static System.Net.Http.HttpClient CreateHttpClient()
    {
        System.Net.Http.HttpClient client = new()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        // GitHub's REST API requires a User-Agent header.
        client.DefaultRequestHeaders.UserAgent.ParseAdd("InfoScreen/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    private sealed class GitHubContentItem
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";

        [JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; } = "";
    }
}
