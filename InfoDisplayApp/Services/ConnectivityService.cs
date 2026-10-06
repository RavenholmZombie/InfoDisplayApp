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
        new("http://www.msftconnecttest.com/connecttest.txt"),
        new("https://www.google.com/generate_204")
    };

    private readonly System.Net.Http.HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };
    private readonly System.Threading.Timer _timer;
    private int _checking;
    private bool _started;

    public bool IsOnline { get; private set; } = true;
    public event EventHandler<bool>? ConnectivityChanged;
    public event EventHandler<ConnectivityProbeEventArgs>? ProbeStatusChanged;

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
            ProbeStatusChanged?.Invoke(this,
                new ConnectivityProbeEventArgs(ConnectivityProbeState.Checking));

            bool online = NetworkInterface.GetIsNetworkAvailable() && await ProbeInternetAsync();

            ProbeStatusChanged?.Invoke(this,
                new ConnectivityProbeEventArgs(
                    online ? ConnectivityProbeState.Succeeded : ConnectivityProbeState.Failed));

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
        for (int i = 0; i < ProbeUris.Length; i++)
        {
            Uri uri = ProbeUris[i];
            bool hasFallback = i < ProbeUris.Length - 1;

            try
            {
                using HttpRequestMessage request = new(HttpMethod.Get, uri);
                request.Headers.CacheControl =
                    new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };

                using HttpResponseMessage response = await _httpClient.SendAsync(
                    request, HttpCompletionOption.ResponseHeadersRead);

                bool probeSucceeded;
                if (i == 0)
                {
                    // Windows NCSI uses this HTTP endpoint and expects an exact
                    // 200 response body of "Microsoft Connect Test".
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Debug.WriteLine(
                            $"CONNECTIVITY: primary probe {uri.Host} returned HTTP {(int)response.StatusCode}; trying fallback probe.");
                        continue;
                    }

                    string body = await response.Content.ReadAsStringAsync();
                    probeSucceeded = string.Equals(
                        body.Trim(), "Microsoft Connect Test", StringComparison.Ordinal);
                }
                else
                {
                    // Google's generate_204 endpoint is the independent fallback.
                    probeSucceeded = response.StatusCode == HttpStatusCode.NoContent;
                }

                if (probeSucceeded)
                {
                    if (i > 0)
                        Debug.WriteLine($"CONNECTIVITY: fallback probe {uri.Host} succeeded. Internet connection is available.");

                    return true;
                }

                Debug.WriteLine(
                    $"CONNECTIVITY: {(i == 0 ? "primary" : "fallback")} probe {uri.Host} returned an unexpected response" +
                    (hasFallback ? "; trying fallback probe." : "."));
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                if (hasFallback)
                {
                    Debug.WriteLine(
                        $"CONNECTIVITY: primary probe {uri.Host} unavailable ({GetProbeFailureSummary(ex)}); trying fallback probe.");
                }
                else
                {
                    Debug.WriteLine(
                        $"CONNECTIVITY: fallback probe {uri.Host} unavailable ({GetProbeFailureSummary(ex)}).");
                }
            }
        }

        Debug.WriteLine("CONNECTIVITY: all Internet connectivity probes failed. Entering Offline Mode.");
        return false;
    }

    private static string GetProbeFailureSummary(Exception ex)
    {
        if (ex is TaskCanceledException)
            return "request timed out";

        if (ex is HttpRequestException http)
        {
            Exception? inner = http.InnerException;
            while (inner?.InnerException != null)
                inner = inner.InnerException;

            if (inner is System.Net.Sockets.SocketException socket)
                return $"network/DNS error: {socket.SocketErrorCode}";

            if (http.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase) ||
                http.Message.Contains("TLS", StringComparison.OrdinalIgnoreCase))
                return "SSL/TLS connection unavailable";

            return "HTTP connection unavailable";
        }

        return "connection unavailable";
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


public enum ConnectivityProbeState
{
    Checking,
    Failed,
    Succeeded
}

public sealed class ConnectivityProbeEventArgs : EventArgs
{
    public ConnectivityProbeEventArgs(ConnectivityProbeState state) => State = state;
    public ConnectivityProbeState State { get; }
}
