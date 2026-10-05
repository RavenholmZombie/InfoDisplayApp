using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;

namespace InfoDisplayApp.Services;

/// <summary>
/// Central Internet-connectivity state for InfoScreen. A single remote service
/// failure never marks the application offline; multiple independent probes
/// must fail before Offline Mode is entered.
/// </summary>
public sealed class ConnectivityService : IDisposable
{
    private static readonly Uri[] ProbeUris =
    {
        new("https://www.msftconnecttest.com/connecttest.txt"),
        new("https://www.google.com/generate_204")
    };

    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };
    private readonly System.Threading.Timer _timer;
    private int _checking;
    private bool _started;

    public bool IsOnline { get; private set; } = true;
    public event EventHandler<bool>? ConnectivityChanged;

    public ConnectivityService()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("InfoScreen/1.0");
        _timer = new System.Threading.Timer(async _ => await CheckAsync(), null,
            Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public async Task StartAsync()
    {
        if (_started) return;
        _started = true;
        await CheckAsync();
        _timer.Change(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public async Task<bool> CheckAsync()
    {
        if (Interlocked.Exchange(ref _checking, 1) != 0)
            return IsOnline;

        try
        {
            bool online = NetworkInterface.GetIsNetworkAvailable() && await ProbeInternetAsync();
            SetState(online);
            return online;
        }
        finally
        {
            Volatile.Write(ref _checking, 0);
        }
    }

    private async Task<bool> ProbeInternetAsync()
    {
        foreach (Uri uri in ProbeUris)
        {
            try
            {
                using HttpRequestMessage request = new(HttpMethod.Get, uri);
                request.Headers.CacheControl =
                    new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };

                using HttpResponseMessage response = await _httpClient.SendAsync(
                    request, HttpCompletionOption.ResponseHeadersRead);

                // Any real HTTP response proves that the Internet path works.
                // This deliberately includes 403/429/500 responses.
                if ((int)response.StatusCode >= 100)
                    return true;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                Debug.WriteLine($"CONNECTIVITY: probe {uri.Host} failed: {ex.Message}");
            }
        }

        return false;
    }

    private void SetState(bool online)
    {
        if (IsOnline == online) return;
        IsOnline = online;
        Debug.WriteLine($"CONNECTIVITY: InfoScreen is now {(online ? "online" : "offline")}.");
        ConnectivityChanged?.Invoke(this, online);
    }

    public void Dispose()
    {
        _timer.Dispose();
        _httpClient.Dispose();
    }
}
