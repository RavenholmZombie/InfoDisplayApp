using System.Diagnostics;
using System.Text.Json;

namespace InfoDisplayApp.Services
{
    internal sealed class AppSettings
    {
        private static readonly object Sync = new();
        private static AppSettings? _current;

        public static AppSettings Current
        {
            get
            {
                lock (Sync)
                    return _current ??= Load();
            }
        }

        public DiagnosticSettings Diagnostics { get; set; } = new();

        private static string SettingsPath =>
            Path.Combine(AppContext.BaseDirectory, "settings.json");

        private static AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    AppSettings defaults = new();
                    string json = JsonSerializer.Serialize(
                        defaults,
                        new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(SettingsPath, json);
                    return defaults;
                }

                string jsonText = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(
                    jsonText,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new AppSettings();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to load settings.json; using defaults: {ex}");
                return new AppSettings();
            }
        }
    }

    internal sealed class DiagnosticSettings
    {
        public bool AudioPathology { get; set; } = false;
        public bool CoreAudioMonitor { get; set; } = false;
        public bool DisplayMonitor { get; set; } = false;
        public bool RemoteServer { get; set; } = true;
        public bool RemoteTelemetry { get; set; } = true;
    }
}
