namespace InfoDisplayApp.Services;

public sealed class AppletDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = "";
    public string Url { get; set; } = "";
    public string IconUrl { get; set; } = "";
    public string Author { get; set; } = "";
    public AppletCapabilities Capabilities { get; set; } = new();
}

public sealed class AppletCapabilities
{
    // Streaming/video applets opt in to periodic WebView2 media-pipeline
    // maintenance. Ordinary web applets are deliberately left untouched.
    public bool MediaPlayback { get; set; }
}
