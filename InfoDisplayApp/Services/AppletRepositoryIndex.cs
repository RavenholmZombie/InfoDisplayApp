namespace InfoDisplayApp.Services;

public sealed class AppletRepositoryIndex
{
    public int SchemaVersion { get; set; } = 1;
    public List<string> Applets { get; set; } = new();
}
