using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace InfoDisplayApp;

internal sealed class RemoteDiagnosticsServer : IDisposable
{
    private const int Port = 8765;
    private readonly TcpListener _listener = new(IPAddress.Any, Port);
    private readonly CancellationTokenSource _cts = new();
    private readonly Stopwatch _uptime = Stopwatch.StartNew();
    private readonly System.Net.Http.HttpClient _go2rtcHttp = new()
    {
        Timeout = TimeSpan.FromMilliseconds(1500)
    };
    private Task? _acceptLoop;
    private Task? _sampleLoop;
    private readonly object _snapshotLock = new();
    private object? _latestSnapshot;
    private long _sampleSequence;
    private TimeSpan _lastCpu;
    private DateTime _lastCpuAt = DateTime.UtcNow;
    private long _lastRx;
    private long _lastTx;
    private DateTime _lastNetworkAt = DateTime.UtcNow;
    private TimeSpan _lastGo2RtcCpu;
    private DateTime _lastGo2RtcCpuAt = DateTime.UtcNow;
    private int _sampleNumber;
    private Go2RtcStreamResult[] _lastGo2RtcStreams = [];
    private Go2RtcProcessResult _lastGo2RtcProcess = new(false, null, null, null, null);
    private NetworkAdapterResult[] _lastAdapters = [];

    public void Start()
    {
        _lastCpu = Process.GetCurrentProcess().TotalProcessorTime;
        (_lastRx, _lastTx) = GetNetworkBytes();
        _listener.Start();
        _acceptLoop = Task.Run(AcceptLoopAsync);
        _sampleLoop = Task.Run(SampleLoopAsync);
        Debug.WriteLine($"Remote diagnostics listening on TCP {Port}.");
    }

    private async Task AcceptLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(_cts.Token);
                _ = Task.Run(() => HandleClientAsync(client));
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex) { Debug.WriteLine($"Diagnostics accept failed: {ex}"); }
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        {
            client.ReceiveTimeout = 5000;
            client.SendTimeout = 5000;
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new(stream, Encoding.ASCII, false, 1024, leaveOpen: true);

            string? requestLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(requestLine))
                return;

            string[] parts = requestLine.Split(' ');
            if (parts.Length < 2)
                return;

            string method = parts[0];
            string path = parts[1];

            string? line;
            do { line = await reader.ReadLineAsync(); }
            while (!string.IsNullOrEmpty(line));

            try
            {
                if (method == "GET" && path == "/api/diagnostics")
                {
                    object? snapshot;
                    lock (_snapshotLock)
                        snapshot = _latestSnapshot;

                    if (snapshot == null)
                    {
                        await WriteJsonAsync(stream, 503, new { error = "Telemetry is warming up." });
                        return;
                    }

                    await WriteJsonAsync(stream, 200, snapshot);
                    return;
                }

                if (method == "POST" && path.StartsWith("/api/control/", StringComparison.Ordinal))
                {
                    string command = path["/api/control/".Length..].ToLowerInvariant();
                    bool accepted = DispatchCommand(command);
                    await WriteJsonAsync(stream, accepted ? 202 : 404,
                        new { accepted, command });
                    return;
                }

                await WriteJsonAsync(stream, 404, new { error = "Not found" });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Diagnostics request failed: {ex}");
                await WriteJsonAsync(stream, 500, new { error = ex.Message });
            }
        }
    }

    private async Task SampleLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                object snapshot = await BuildSnapshotAsync();
                lock (_snapshotLock)
                    _latestSnapshot = snapshot;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Diagnostic sample failed: {ex}");
            }

            try { await Task.Delay(1000, _cts.Token); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task<object> BuildSnapshotAsync()
    {
        Process process = Process.GetCurrentProcess();
        DateTime now = DateTime.UtcNow;

        TimeSpan cpuNow = process.TotalProcessorTime;
        double elapsedMs = Math.Max(1, (now - _lastCpuAt).TotalMilliseconds);
        double cpu = (cpuNow - _lastCpu).TotalMilliseconds / elapsedMs /
                     Environment.ProcessorCount * 100.0;
        _lastCpu = cpuNow;
        _lastCpuAt = now;

        (long rx, long tx) = GetNetworkBytes();
        double networkSeconds = Math.Max(0.001, (now - _lastNetworkAt).TotalSeconds);
        double rxMbps = Math.Max(0, rx - _lastRx) * 8.0 / networkSeconds / 1_000_000.0;
        double txMbps = Math.Max(0, tx - _lastTx) * 8.0 / networkSeconds / 1_000_000.0;
        _lastRx = rx;
        _lastTx = tx;
        _lastNetworkAt = now;

        string? gateway = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                        n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(n => n.GetIPProperties().GatewayAddresses)
            .Select(g => g.Address)
            .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
            ?.ToString();

        PingResult gatewayPing = gateway == null
            ? new(false, null)
            : await PingAsync(gateway);
        PingResult internetPing = await PingAsync("1.1.1.1");

        ServiceResult go2rtcApi = await TcpProbeAsync("127.0.0.1", 1984);
        ServiceResult go2rtcRtsp = await TcpProbeAsync("127.0.0.1", 8554);
        // Expensive/diagnostic-only inspection runs every 5th sample. The core
        // reachability probes stay at 1 Hz, but process enumeration, adapter
        // statistics and go2rtc API parsing should not compete with WinForms
        // painting/media playback every second.
        if (++_sampleNumber == 1 || _sampleNumber % 5 == 0)
        {
            _lastGo2RtcStreams =
            [
                await ProbeGo2RtcStreamAsync("cheddar_camera"),
                await ProbeGo2RtcStreamAsync("front_door")
            ];
            _lastGo2RtcProcess = GetGo2RtcProcessStats(now);
            _lastAdapters = GetNetworkAdapterStats();
        }

        Go2RtcStreamResult[] go2rtcStreams = _lastGo2RtcStreams;
        Go2RtcProcessResult go2rtcProcess = _lastGo2RtcProcess;
        NetworkAdapterResult[] adapters = _lastAdapters;

        CameraResult[] cameras =
        [
            await ProbeCameraAsync("Cheddar", "192.168.40.207"),
            await ProbeCameraAsync("Pet", "192.168.40.220"),
            await ProbeCameraAsync("Doorbell", "192.168.40.233")
        ];

        return new
        {
            sequence = Interlocked.Increment(ref _sampleSequence),
            timestampUtc = now,
            server = new
            {
                machineName = Environment.MachineName,
                processId = Environment.ProcessId,
                cpuPercent = Math.Round(cpu, 1),
                workingSetMb = Math.Round(process.WorkingSet64 / 1024d / 1024d, 1),
                uptimeSeconds = Math.Round(_uptime.Elapsed.TotalSeconds),
                threadCount = process.Threads.Count
            },
            network = new
            {
                gateway,
                gatewayOnline = gatewayPing.Online,
                gatewayLatencyMs = gatewayPing.LatencyMs,
                internetOnline = internetPing.Online,
                internetLatencyMs = internetPing.LatencyMs,
                receiveMbps = Math.Round(rxMbps, 2),
                transmitMbps = Math.Round(txMbps, 2),
                adapters
            },
            go2rtc = new
            {
                apiOnline = go2rtcApi.Online,
                apiLatencyMs = go2rtcApi.LatencyMs,
                rtspOnline = go2rtcRtsp.Online,
                rtspLatencyMs = go2rtcRtsp.LatencyMs,
                process = go2rtcProcess,
                streams = go2rtcStreams
            },
            cameras
        };
    }

    private Go2RtcProcessResult GetGo2RtcProcessStats(DateTime now)
    {
        try
        {
            Process[] processes = Process.GetProcessesByName("go2rtc");
            try
            {
                Process? go2rtc = processes
                    .OrderByDescending(p => p.WorkingSet64)
                    .FirstOrDefault();

                if (go2rtc == null)
                    return new(false, null, null, null, null);

                TimeSpan cpuNow = go2rtc.TotalProcessorTime;
                double elapsedMs = Math.Max(1, (now - _lastGo2RtcCpuAt).TotalMilliseconds);
                double cpu = (cpuNow - _lastGo2RtcCpu).TotalMilliseconds /
                             elapsedMs / Environment.ProcessorCount * 100.0;

                _lastGo2RtcCpu = cpuNow;
                _lastGo2RtcCpuAt = now;

                return new(true, go2rtc.Id, Math.Round(Math.Max(0, cpu), 1),
                    Math.Round(go2rtc.WorkingSet64 / 1024d / 1024d, 1),
                    go2rtc.Threads.Count);
            }
            finally
            {
                foreach (Process process in processes)
                    process.Dispose();
            }
        }
        catch { return new(false, null, null, null, null); }
    }

    private static NetworkAdapterResult[] GetNetworkAdapterStats()
    {
        List<NetworkAdapterResult> results = [];
        foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces()
                     .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                                 n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
        {
            try
            {
                IPv4InterfaceStatistics stats = nic.GetIPv4Statistics();
                string[] addresses = nic.GetIPProperties().UnicastAddresses
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(a => a.Address.ToString()).ToArray();
                results.Add(new(nic.Name, nic.Description, nic.NetworkInterfaceType.ToString(),
                    nic.Speed, addresses, stats.BytesReceived, stats.BytesSent,
                    stats.IncomingPacketsWithErrors, stats.OutgoingPacketsWithErrors,
                    stats.IncomingPacketsDiscarded, stats.OutgoingPacketsDiscarded));
            }
            catch (NetworkInformationException) { }
        }
        return results.ToArray();
    }

    private async Task<Go2RtcStreamResult> ProbeGo2RtcStreamAsync(string name)
    {
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            string url = $"http://127.0.0.1:1984/api/streams?src={Uri.EscapeDataString(name)}";
            using System.Net.Http.HttpResponseMessage response = await _go2rtcHttp.GetAsync(url);
            string body = await response.Content.ReadAsStringAsync();

            bool registered = response.IsSuccessStatusCode &&
                              !string.IsNullOrWhiteSpace(body) &&
                              body != "{}" &&
                              body.Contains(name, StringComparison.OrdinalIgnoreCase);

            int producers = 0;
            int consumers = 0;
            try
            {
                using JsonDocument json = JsonDocument.Parse(body);
                CountGo2RtcEndpoints(json.RootElement, ref producers, ref consumers);
            }
            catch (JsonException) { }

            return new(name, response.IsSuccessStatusCode, registered,
                sw.ElapsedMilliseconds, producers, consumers);
        }
        catch
        {
            return new(name, false, false, null, 0, 0);
        }
    }

    private static void CountGo2RtcEndpoints(
        JsonElement element, ref int producers, ref int consumers)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (property.NameEquals("producers") &&
                    property.Value.ValueKind == JsonValueKind.Array)
                    producers += property.Value.GetArrayLength();
                else if (property.NameEquals("consumers") &&
                         property.Value.ValueKind == JsonValueKind.Array)
                    consumers += property.Value.GetArrayLength();

                CountGo2RtcEndpoints(property.Value, ref producers, ref consumers);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement child in element.EnumerateArray())
                CountGo2RtcEndpoints(child, ref producers, ref consumers);
        }
    }

    private static async Task<CameraResult> ProbeCameraAsync(string name, string address)
    {
        PingResult ping = await PingAsync(address);
        ServiceResult rtsp = await TcpProbeAsync(address, 554);
        return new(name, address, ping.Online, ping.LatencyMs, rtsp.Online);
    }

    private static async Task<PingResult> PingAsync(string host)
    {
        try
        {
            using Ping ping = new();
            PingReply reply = await ping.SendPingAsync(host, 1500);
            return new(reply.Status == IPStatus.Success,
                reply.Status == IPStatus.Success ? reply.RoundtripTime : null);
        }
        catch { return new(false, null); }
    }

    private static async Task<ServiceResult> TcpProbeAsync(string host, int port)
    {
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            using TcpClient client = new();
            Task connect = client.ConnectAsync(host, port);
            if (await Task.WhenAny(connect, Task.Delay(1500)) != connect)
            {
                _ = connect.ContinueWith(t => _ = t.Exception,
                    TaskContinuationOptions.OnlyOnFaulted);
                return new(false, null);
            }
            await connect;
            return new(true, sw.ElapsedMilliseconds);
        }
        catch { return new(false, null); }
    }

    private static (long Rx, long Tx) GetNetworkBytes()
    {
        long rx = 0, tx = 0;
        foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces()
                     .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                                 n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
        {
            IPv4InterfaceStatistics stats = nic.GetIPv4Statistics();
            rx += stats.BytesReceived;
            tx += stats.BytesSent;
        }
        return (rx, tx);
    }

    private static bool DispatchCommand(string command)
    {
        frmMain? form = Application.OpenForms.OfType<frmMain>().FirstOrDefault();
        if (form == null || form.IsDisposed)
            return false;

        form.BeginInvoke(() =>
        {
            switch (command)
            {
                case "restart":
                    form.PrepareForShutdown();
                    Application.Restart();
                    break;
                case "shutdown":
                    form.PrepareForShutdown();
                    Application.Exit();
                    break;
                case "kill":
                    Process.GetCurrentProcess().Kill();
                    break;
            }
        });

        return command is "restart" or "shutdown" or "kill";
    }

    private static async Task WriteJsonAsync(NetworkStream stream, int status, object value)
    {
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(value);
        string reason = status switch { 200 => "OK", 202 => "Accepted", 404 => "Not Found", _ => "Error" };
        byte[] header = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 {status} {reason}\r\nContent-Type: application/json\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(header);
        await stream.WriteAsync(body);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
        _go2rtcHttp.Dispose();
        _cts.Dispose();
    }

    private sealed record PingResult(bool Online, long? LatencyMs);
    private sealed record ServiceResult(bool Online, long? LatencyMs);
    private sealed record NetworkAdapterResult(
        string Name, string Description, string Type, long LinkSpeedBitsPerSecond,
        string[] Addresses, long BytesReceived, long BytesSent,
        long ReceiveErrors, long TransmitErrors, long ReceiveDiscards, long TransmitDiscards);
    private sealed record Go2RtcProcessResult(
        bool Running, int? ProcessId, double? CpuPercent, double? WorkingSetMb, int? ThreadCount);
    private sealed record Go2RtcStreamResult(
        string Name,
        bool ApiResponding,
        bool Registered,
        long? ApiLatencyMs,
        int Producers,
        int Consumers);
    private sealed record CameraResult(
        string Name,
        string Address,
        bool Online,
        long? LatencyMs,
        bool RtspOnline);
}
