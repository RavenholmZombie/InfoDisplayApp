using System.Net.Http.Json;
using System.Text.Json;

namespace InfoDisplayApp.Services;

public sealed class AppletRepositoryService
{
    private const string RawRoot = "https://raw.githubusercontent.com/RavenholmZombie/InfoScreenAppRepository/main/";
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<AppletDefinition>> GetAvailableAppletsAsync(CancellationToken cancellationToken = default)
    {
        AppletRepositoryIndex? index = await _httpClient.GetFromJsonAsync<AppletRepositoryIndex>(
            RawRoot + "repository.json", JsonOptions, cancellationToken);
        if (index == null) return Array.Empty<AppletDefinition>();

        List<AppletDefinition> result = new();
        foreach (string relativePath in index.Applets)
        {
            try
            {
                AppletDefinition? applet = await _httpClient.GetFromJsonAsync<AppletDefinition>(
                    new Uri(new Uri(RawRoot), relativePath), JsonOptions, cancellationToken);
                if (applet != null && AppletManager.IsValid(applet)) result.Add(applet);
            }
            catch { }
        }
        return result.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
