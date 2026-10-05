using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InfoDisplayApp.Services;

public sealed class AppletRepositoryService
{
    private const string RepositoryApiRoot = "https://api.github.com/repos/RavenholmZombie/InfoScreenAppRepository";
    private const string AppletsApiUrl = RepositoryApiRoot + "/contents/applets?ref=main";

    private static string CacheBust(string url) =>
        url + (url.Contains('?') ? "&" : "?") + "_infoscreen=" +
        Guid.NewGuid().ToString("N");

    private readonly System.Net.Http.HttpClient _httpClient = CreateHttpClient();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<AppletDefinition>> GetAvailableAppletsAsync(CancellationToken cancellationToken = default)
    {
        // Resolve main to its current commit first, then read every applet by that
        // immutable commit SHA. This avoids raw.githubusercontent.com/CDN caching
        // and guarantees one internally-consistent repository snapshot per check.
        GitHubBranch? branch = await GetJsonAsync<GitHubBranch>(
            CacheBust(RepositoryApiRoot + "/branches/main"), cancellationToken);
        string commitSha = branch?.Commit?.Sha ?? "";

        if (string.IsNullOrWhiteSpace(commitSha))
            throw new InvalidDataException("GitHub did not return the current InfoScreen applet repository commit.");

        IReadOnlyList<GitHubContentItem>? files =
            await GetJsonAsync<IReadOnlyList<GitHubContentItem>>(
                CacheBust(RepositoryApiRoot + "/contents/applets?ref=" + Uri.EscapeDataString(commitSha)),
                cancellationToken);

        if (files == null)
            return Array.Empty<AppletDefinition>();

        List<AppletDefinition> result = new();

        foreach (GitHubContentItem file in files.Where(item =>
                     item.Type.Equals("file", StringComparison.OrdinalIgnoreCase) &&
                     item.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                AppletDefinition? applet = await GetJsonAsync<AppletDefinition>(
                    CacheBust(RepositoryApiRoot + "/contents/applets/" +
                        Uri.EscapeDataString(file.Name) + "?ref=" + Uri.EscapeDataString(commitSha)),
                    cancellationToken);

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

    private async Task<T?> GetJsonAsync<T>(string url, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true,
            MaxAge = TimeSpan.Zero
        };
        request.Headers.Pragma.ParseAdd("no-cache");

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        // GitHub's Contents API returns file bodies as base64 JSON metadata rather
        // than the raw file. Decode that payload when T is an applet definition.
        if (typeof(T) == typeof(AppletDefinition))
        {
            GitHubFileContent? file =
                await response.Content.ReadFromJsonAsync<GitHubFileContent>(JsonOptions, cancellationToken);

            if (file == null || string.IsNullOrWhiteSpace(file.Content))
                return default;

            string compact = file.Content.Replace("\r", "").Replace("\n", "");
            byte[] bytes = Convert.FromBase64String(compact);
            AppletDefinition? applet = JsonSerializer.Deserialize<AppletDefinition>(bytes, JsonOptions);
            return (T?)(object?)applet;
        }

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<AppletUpdate>> GetUpdatesAsync(
        AppletManager manager,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AppletDefinition> installed = manager.GetInstalledApplets();
        if (installed.Count == 0)
            return Array.Empty<AppletUpdate>();

        IReadOnlyList<AppletDefinition> available = await GetAvailableAppletsAsync(cancellationToken);
        Dictionary<string, AppletDefinition> repository = available
            .ToDictionary(a => a.Id, StringComparer.OrdinalIgnoreCase);

        List<AppletUpdate> updates = new();
        foreach (AppletDefinition local in installed)
        {
            if (repository.TryGetValue(local.Id, out AppletDefinition? remote) &&
                CompareVersions(remote.Version, local.Version) > 0)
            {
                updates.Add(new AppletUpdate(local, remote));
            }
        }

        return updates.OrderBy(u => u.Available.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static int CompareVersions(string? left, string? right)
    {
        static int[] Parts(string? value) =>
            (value ?? "")
            .Split(new[] { '.', '-', '+' }, StringSplitOptions.RemoveEmptyEntries)
            .TakeWhile(part => int.TryParse(part, out _))
            .Select(int.Parse)
            .ToArray();

        int[] a = Parts(left);
        int[] b = Parts(right);
        int count = Math.Max(a.Length, b.Length);

        for (int i = 0; i < count; i++)
        {
            int av = i < a.Length ? a[i] : 0;
            int bv = i < b.Length ? b[i] : 0;
            int comparison = av.CompareTo(bv);
            if (comparison != 0)
                return comparison;
        }

        return string.Compare(left ?? "", right ?? "", StringComparison.OrdinalIgnoreCase);
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
        client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true,
            MaxAge = TimeSpan.Zero
        };
        client.DefaultRequestHeaders.Pragma.ParseAdd("no-cache");
        return client;
    }

    private sealed class GitHubContentItem
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
    }

    private sealed class GitHubBranch
    {
        public GitHubCommit? Commit { get; set; }
    }

    private sealed class GitHubCommit
    {
        public string Sha { get; set; } = "";
    }

    private sealed class GitHubFileContent
    {
        public string Content { get; set; } = "";
    }
}


public sealed record AppletUpdate(AppletDefinition Installed, AppletDefinition Available);
