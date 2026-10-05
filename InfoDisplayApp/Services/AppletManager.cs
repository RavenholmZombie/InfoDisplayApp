using System.Text.Json;

namespace InfoDisplayApp.Services;

public sealed class AppletManager
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    private static readonly System.Net.Http.HttpClient IconClient = new() { Timeout = TimeSpan.FromSeconds(15) };

    public string AppletsDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "applets");
    public string IconsDirectory => Path.Combine(AppletsDirectory, "icons");

    public AppletManager()
    {
        Directory.CreateDirectory(AppletsDirectory);
        Directory.CreateDirectory(IconsDirectory);
    }

    public IReadOnlyList<AppletDefinition> GetInstalledApplets()
    {
        Directory.CreateDirectory(AppletsDirectory);
        List<AppletDefinition> applets = new();
        foreach (string file in Directory.EnumerateFiles(AppletsDirectory, "*.json"))
        {
            try
            {
                AppletDefinition? applet = JsonSerializer.Deserialize<AppletDefinition>(File.ReadAllText(file), JsonOptions);
                if (applet != null && IsValid(applet)) applets.Add(applet);
            }
            catch { }
        }
        return applets.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public bool IsInstalled(string id) => GetInstalledApplets().Any(a => a.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public async Task InstallAsync(AppletDefinition applet)
    {
        if (!IsValid(applet)) throw new InvalidDataException("The applet definition is incomplete or invalid.");
        string destination = Path.Combine(AppletsDirectory, GetSafeId(applet.Id) + ".json");
        await File.WriteAllTextAsync(destination, JsonSerializer.Serialize(applet, JsonOptions));

        if (Uri.TryCreate(applet.IconUrl, UriKind.Absolute, out Uri? iconUri) &&
            (iconUri.Scheme == Uri.UriSchemeHttp || iconUri.Scheme == Uri.UriSchemeHttps))
        {
            try
            {
                byte[] iconBytes = await IconClient.GetByteArrayAsync(iconUri);
                string extension = Path.GetExtension(iconUri.AbsolutePath);
                if (string.IsNullOrWhiteSpace(extension) || extension.Length > 8) extension = ".img";
                await File.WriteAllBytesAsync(Path.Combine(IconsDirectory, GetSafeId(applet.Id) + extension), iconBytes);
            }
            catch { }
        }
    }

    public async Task UpdateAsync(AppletDefinition applet)
    {
        if (!IsValid(applet))
            throw new InvalidDataException("The applet definition is incomplete or invalid.");

        string destination = Path.Combine(AppletsDirectory, GetSafeId(applet.Id) + ".json");

        // Updates intentionally replace the installed definition rather than
        // merging it, so removed/renamed repository fields cannot linger.
        if (File.Exists(destination))
            File.Delete(destination);

        await InstallAsync(applet);
    }

    public string? GetCachedIconPath(string id)
    {
        string prefix = GetSafeId(id) + ".";
        return Directory.EnumerateFiles(IconsDirectory)
            .FirstOrDefault(file => Path.GetFileName(file).StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    public void Uninstall(string id)
    {
        string destination = Path.Combine(AppletsDirectory, GetSafeId(id) + ".json");
        if (File.Exists(destination)) File.Delete(destination);

        string? icon = GetCachedIconPath(id);
        if (icon != null && File.Exists(icon)) File.Delete(icon);
    }

    public static bool IsValid(AppletDefinition applet) =>
        !string.IsNullOrWhiteSpace(applet.Id) && !string.IsNullOrWhiteSpace(applet.Name) &&
        Uri.TryCreate(applet.Url, UriKind.Absolute, out Uri? uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static string GetSafeId(string id)
    {
        string safe = new(id.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
        if (string.IsNullOrWhiteSpace(safe)) throw new InvalidDataException("Applet ID does not contain a usable filename.");
        return safe;
    }
}
