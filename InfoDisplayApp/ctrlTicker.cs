using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MineStatLib;

namespace InfoDisplayApp.Properties
{
    public partial class ctrlTicker : UserControl
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private readonly System.Windows.Forms.Timer _reloadTimer;
        private readonly System.Windows.Forms.Timer _statusTimer;
        private readonly System.Windows.Forms.Timer _weatherTimer;
        private readonly System.Threading.Timer _animationTimer;

        private int _animationFramePending;
        private bool _animationRunning;
        private bool _timerResolutionRequested;
        private bool _weatherUpdating;

        private string _rycraftStatus = "Checking...";
        private int _rycraftPlayersOnline;
        private int _rycraftPlayersMax;
        private string _rycraftPlayerNames = "Unavailable";
        private string _tapoStatus = "Checking...";

        private string _princetonForecast = "Weather unavailable";
        private string _baileyvilleForecast = "Weather unavailable";
        private string _calaisForecast = "Weather unavailable";

        private string _rycraftHost = "";
        private int _rycraftPort = 25565;
        private string _rycraftLocalHost = "";
        private int _rycraftLocalPort = 25565;
        private string _rycraftRconHost = "";
        private int _rycraftRconPort = 25575;
        private string _rycraftRconPassword = "";

        private const string CheddarCameraIp = "192.168.40.210";
        private const string DenCameraIp = "192.168.40.209";
        private const string DoorbellCameraIp = "192.168.40.233";
        private const int CameraPingTimeoutMilliseconds = 1500;

        private static readonly WeatherLocation Princeton =
            new("Princeton, ME", 45.143109, -67.526589);

        private static readonly WeatherLocation Baileyville =
            new("Baileyville, ME", 45.15529, -67.40888);

        private static readonly WeatherLocation Calais =
            new("Calais, ME", 45.18829, -67.27664);

        private string StatusConfigPath =>
            Path.Combine(AppContext.BaseDirectory, "status.conf");

        private string TickerPath =>
            Path.Combine(AppContext.BaseDirectory, "ticker.txt");

        private readonly List<string> _messages = new();
        private int _currentMessageIndex;

        private readonly Stopwatch _scrollClock = new();
        private double _lastScrollSeconds;
        private double _scrollX;
        private float _messageWidth;
        private string _renderedMessage = "";

        private const double ScrollPixelsPerSecond = 240.0;
        private const int AnimationPulseMilliseconds = 8;
        private const int MessageGap = 50;
        private const uint TimerResolutionMilliseconds = 1;

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint TimeBeginPeriod(uint period);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern uint TimeEndPeriod(uint period);

        public ctrlTicker()
        {
            InitializeComponent();

            lblTextTicker.Visible = false;
            panel1.Paint += panel1_Paint;
            panel1.Resize += panel1_Resize;

            _animationTimer = new System.Threading.Timer(
                AnimationTimerCallback,
                null,
                Timeout.Infinite,
                Timeout.Infinite);

            _reloadTimer = new System.Windows.Forms.Timer
            {
                Interval = 10_000
            };
            _reloadTimer.Tick += ReloadTimer_Tick;

            _statusTimer = new System.Windows.Forms.Timer
            {
                Interval = 30_000
            };
            _statusTimer.Tick += StatusTimer_Tick;

            _weatherTimer = new System.Windows.Forms.Timer
            {
                Interval = 15 * 60 * 1000
            };
            _weatherTimer.Tick += WeatherTimer_Tick;

            Load += ctrlTicker_Load;
            Disposed += ctrlTicker_Disposed;
        }

        private async void ctrlTicker_Load(object? sender, EventArgs e)
        {
            _timerResolutionRequested =
                TimeBeginPeriod(TimerResolutionMilliseconds) == 0;

            LoadStatusConfiguration();

            await Task.WhenAll(
                UpdateStatusesAsync(),
                UpdateForecastsAsync());

            LoadTickerMessages();

            if (_messages.Count > 0)
            {
                ShowCurrentMessage();
                StartAnimation();
            }

            _reloadTimer.Start();
            _statusTimer.Start();
            _weatherTimer.Start();
        }

        private void LoadTickerMessages()
        {
            try
            {
                if (!File.Exists(TickerPath))
                {
                    Debug.WriteLine($"Ticker file not found: {TickerPath}");
                    ClearTicker();
                    return;
                }

                List<string> newMessages = File.ReadAllLines(TickerPath)
                    .Select(line => line.Trim())
                    .Where(line =>
                        !string.IsNullOrWhiteSpace(line) &&
                        !line.StartsWith("#"))
                    .ToList();

                if (newMessages.Count == 0)
                {
                    ClearTicker();
                    return;
                }

                if (_messages.SequenceEqual(newMessages))
                    return;

                _messages.Clear();
                _messages.AddRange(newMessages);
                _currentMessageIndex = 0;
                ShowCurrentMessage();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to load ticker.txt: {ex}");
            }
        }

        private void ClearTicker()
        {
            _messages.Clear();
            _renderedMessage = "";
            StopAnimation();
            panel1.Invalidate();
            panel1.Update();
        }

        private void ShowCurrentMessage()
        {
            if (_messages.Count == 0)
            {
                _renderedMessage = "";
                panel1.Invalidate();
                return;
            }

            if (_currentMessageIndex >= _messages.Count)
                _currentMessageIndex = 0;

            _renderedMessage = _messages[_currentMessageIndex]
                .Replace(
                    "{RYCRAFT_STATUS}",
                    _rycraftStatus,
                    StringComparison.OrdinalIgnoreCase)
                .Replace(
                    "{RYCRAFT_PLAYERS}",
                    _rycraftPlayersOnline >= 0 && _rycraftPlayersMax >= 0
                        ? $"{_rycraftPlayersOnline}/{_rycraftPlayersMax}"
                        : "Unavailable",
                    StringComparison.OrdinalIgnoreCase)
                .Replace(
                    "{RYCRAFT_ONLINE_PLAYERS}",
                    _rycraftPlayersOnline.ToString(),
                    StringComparison.OrdinalIgnoreCase)
                .Replace(
                    "{RYCRAFT_MAX_PLAYERS}",
                    _rycraftPlayersMax.ToString(),
                    StringComparison.OrdinalIgnoreCase)
                .Replace(
                    "{RYCRAFT_PLAYER_NAMES}",
                    _rycraftPlayerNames,
                    StringComparison.OrdinalIgnoreCase)
                .Replace(
                    "{TAPO_STATUS}",
                    _tapoStatus,
                    StringComparison.OrdinalIgnoreCase)
                .Replace(
                    "{LOCAL_FORECAST}",
                    _princetonForecast,
                    StringComparison.OrdinalIgnoreCase)
                .Replace(
                    "{PRINCETON_FORECAST}",
                    _princetonForecast,
                    StringComparison.OrdinalIgnoreCase)
                .Replace(
                    "{BAILEYVILLE_FORECAST}",
                    _baileyvilleForecast,
                    StringComparison.OrdinalIgnoreCase)
                .Replace(
                    "{CALAIS_FORECAST}",
                    _calaisForecast,
                    StringComparison.OrdinalIgnoreCase);

            using Graphics graphics = panel1.CreateGraphics();
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            _messageWidth = graphics.MeasureString(
                _renderedMessage,
                lblTextTicker.Font,
                int.MaxValue,
                StringFormat.GenericTypographic).Width + 10f;

            _scrollX = panel1.ClientSize.Width + MessageGap;

            ResetScrollClock();
            panel1.Invalidate();
            panel1.Update();
        }

        private void StartAnimation()
        {
            if (_animationRunning || IsDisposed)
                return;

            _animationRunning = true;
            ResetScrollClock();
            _animationTimer.Change(0, AnimationPulseMilliseconds);
        }

        private void StopAnimation()
        {
            if (!_animationRunning)
                return;

            _animationRunning = false;
            _animationTimer.Change(Timeout.Infinite, Timeout.Infinite);
            Interlocked.Exchange(ref _animationFramePending, 0);
        }

        private void AnimationTimerCallback(object? state)
        {
            if (!_animationRunning ||
                IsDisposed ||
                Disposing ||
                !IsHandleCreated)
            {
                return;
            }

            if (Interlocked.Exchange(ref _animationFramePending, 1) != 0)
                return;

            try
            {
                BeginInvoke(new Action(RenderAnimationFrame));
            }
            catch (ObjectDisposedException)
            {
                Interlocked.Exchange(ref _animationFramePending, 0);
            }
            catch (InvalidOperationException)
            {
                Interlocked.Exchange(ref _animationFramePending, 0);
            }
        }

        private void RenderAnimationFrame()
        {
            try
            {
                if (!_animationRunning ||
                    IsDisposed ||
                    _messages.Count == 0)
                {
                    return;
                }

                double now = _scrollClock.Elapsed.TotalSeconds;
                double elapsed = Math.Clamp(
                    now - _lastScrollSeconds,
                    0.0,
                    0.050);

                _lastScrollSeconds = now;
                _scrollX -= ScrollPixelsPerSecond * elapsed;

                panel1.Invalidate();
                panel1.Update();

                if (_scrollX + _messageWidth < 0)
                {
                    _currentMessageIndex =
                        (_currentMessageIndex + 1) % _messages.Count;

                    ShowCurrentMessage();
                }
            }
            finally
            {
                Interlocked.Exchange(ref _animationFramePending, 0);
            }
        }

        private void panel1_Paint(object? sender, PaintEventArgs e)
        {
            if (string.IsNullOrEmpty(_renderedMessage))
                return;

            e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            using SolidBrush brush = new(lblTextTicker.ForeColor);
            using StringFormat format =
                new(StringFormat.GenericTypographic)
                {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Near,
                    FormatFlags = StringFormatFlags.NoWrap
                };

            e.Graphics.DrawString(
                _renderedMessage,
                lblTextTicker.Font,
                brush,
                new RectangleF(
                    (float)_scrollX,
                    0,
                    Math.Max(_messageWidth, 1f),
                    panel1.ClientSize.Height),
                format);
        }

        private void panel1_Resize(object? sender, EventArgs e) =>
            panel1.Invalidate();

        private void ResetScrollClock()
        {
            _scrollClock.Restart();
            _lastScrollSeconds = 0;
        }

        private void ReloadTimer_Tick(object? sender, EventArgs e)
        {
            LoadTickerMessages();

            if (_messages.Count > 0 && !_animationRunning)
            {
                ShowCurrentMessage();
                StartAnimation();
            }

            if (_messages.Count == 0)
                StopAnimation();
        }

        private void ctrlTicker_Disposed(object? sender, EventArgs e)
        {
            StopAnimation();

            _reloadTimer.Stop();
            _statusTimer.Stop();
            _weatherTimer.Stop();

            _animationTimer.Dispose();
            _reloadTimer.Dispose();
            _statusTimer.Dispose();
            _weatherTimer.Dispose();

            _scrollClock.Stop();

            if (_timerResolutionRequested)
            {
                TimeEndPeriod(TimerResolutionMilliseconds);
                _timerResolutionRequested = false;
            }
        }

        private async Task UpdateForecastsAsync()
        {
            if (_weatherUpdating)
                return;

            _weatherUpdating = true;

            try
            {
                Task<string> princetonTask = GetForecastAsync(Princeton);
                Task<string> baileyvilleTask = GetForecastAsync(Baileyville);
                Task<string> calaisTask = GetForecastAsync(Calais);

                await Task.WhenAll(
                    princetonTask,
                    baileyvilleTask,
                    calaisTask);

                _princetonForecast = princetonTask.Result;
                _baileyvilleForecast = baileyvilleTask.Result;
                _calaisForecast = calaisTask.Result;

                Debug.WriteLine($"Ticker forecast: {_princetonForecast}");
                Debug.WriteLine($"Ticker forecast: {_baileyvilleForecast}");
                Debug.WriteLine($"Ticker forecast: {_calaisForecast}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ticker weather update failed: {ex}");

                const string unavailable =
                    "Weather is temporarily unavailable | Source: Open-Meteo";

                _princetonForecast = $"Princeton, ME: {unavailable}";
                _baileyvilleForecast = $"Baileyville, ME: {unavailable}";
                _calaisForecast = $"Calais, ME: {unavailable}";
            }
            finally
            {
                _weatherUpdating = false;
            }
        }

        private static async Task<string> GetForecastAsync(
            WeatherLocation location)
        {
            string url =
                "https://api.open-meteo.com/v1/forecast" +
                $"?latitude={location.Latitude}" +
                $"&longitude={location.Longitude}" +
                "&current=temperature_2m,weather_code" +
                "&hourly=weather_code" +
                "&daily=weather_code,temperature_2m_max,temperature_2m_min" +
                "&temperature_unit=fahrenheit" +
                "&timezone=America%2FNew_York" +
                "&forecast_days=2";

            string json = await _httpClient.GetStringAsync(url);

            using JsonDocument document = JsonDocument.Parse(json);

            JsonElement root = document.RootElement;
            JsonElement current = root.GetProperty("current");
            JsonElement daily = root.GetProperty("daily");
            JsonElement hourly = root.GetProperty("hourly");

            double currentTemperature =
                current.GetProperty("temperature_2m").GetDouble();

            int currentWeatherCode =
                current.GetProperty("weather_code").GetInt32();

            int todayWeatherCode =
                daily.GetProperty("weather_code")[0].GetInt32();

            double todayHigh =
                daily.GetProperty("temperature_2m_max")[0].GetDouble();

            JsonElement lows =
                daily.GetProperty("temperature_2m_min");

            double tonightLow = lows.GetArrayLength() > 1
                ? lows[1].GetDouble()
                : lows[0].GetDouble();

            string todayDate =
                daily.GetProperty("time")[0].GetString() ??
                DateTime.Now.ToString("yyyy-MM-dd");

            int tonightWeatherCode =
                GetTonightWeatherCode(
                    hourly,
                    todayDate,
                    todayWeatherCode);

            return
                $"Currently in {location.Name}: " +
                $"{GetWeatherDescription(currentWeatherCode)}, " +
                $"{Math.Round(currentTemperature):0}°F | " +
                $"Today's Forecast: {GetWeatherDescription(todayWeatherCode)}, " +
                $"High: {Math.Round(todayHigh):0}°F | " +
                $"Tonight: {GetWeatherDescription(tonightWeatherCode)}, " +
                $"Low: {Math.Round(tonightLow):0}°F | " +
                "Source: Open-Meteo";
        }

        private static int GetTonightWeatherCode(
            JsonElement hourly,
            string todayDate,
            int fallbackCode)
        {
            JsonElement times = hourly.GetProperty("time");
            JsonElement codes = hourly.GetProperty("weather_code");

            DateTime today = DateTime.Parse(todayDate);
            DateTime windowStart = today.Date.AddHours(18);
            DateTime windowEnd = today.Date.AddDays(1).AddHours(6);

            int selectedCode = fallbackCode;
            int selectedSeverity = -1;

            for (int i = 0; i < times.GetArrayLength(); i++)
            {
                string? timeText = times[i].GetString();

                if (timeText == null ||
                    !DateTime.TryParse(timeText, out DateTime time))
                {
                    continue;
                }

                if (time < windowStart || time >= windowEnd)
                    continue;

                int code = codes[i].GetInt32();
                int severity = GetWeatherSeverity(code);

                if (severity > selectedSeverity)
                {
                    selectedCode = code;
                    selectedSeverity = severity;
                }
            }

            return selectedCode;
        }

        private static int GetWeatherSeverity(int code) => code switch
        {
            96 or 99 => 100,
            95 => 95,
            82 => 90,
            86 => 88,
            75 => 86,
            65 => 84,
            67 => 82,
            81 => 80,
            73 => 78,
            63 => 76,
            66 => 74,
            85 => 72,
            80 => 70,
            71 => 68,
            57 => 66,
            55 => 64,
            56 => 62,
            53 => 60,
            61 => 58,
            51 => 56,
            77 => 54,
            45 or 48 => 40,
            3 => 30,
            2 => 20,
            1 => 10,
            _ => 0
        };

        private static string GetWeatherDescription(int code) => code switch
        {
            0 => "Clear Skies",
            1 => "Mostly Clear",
            2 => "Partly Cloudy",
            3 => "Cloudy",
            45 or 48 => "Foggy",
            51 => "Light Drizzle",
            53 => "Drizzle",
            55 => "Heavy Drizzle",
            56 or 57 => "Freezing Drizzle",
            61 => "Light Rain",
            63 => "Rain",
            65 => "Heavy Rain",
            66 or 67 => "Freezing Rain",
            71 => "Light Snow",
            73 => "Snow",
            75 => "Heavy Snow",
            77 => "Snow Grains",
            80 => "Light Showers",
            81 => "Showers",
            82 => "Heavy Showers",
            85 => "Snow Showers",
            86 => "Heavy Snow Showers",
            95 => "Thunderstorms",
            96 or 99 => "Severe Thunderstorms",
            _ => "Unknown Conditions"
        };

        private async void WeatherTimer_Tick(object? sender, EventArgs e)
        {
            _weatherTimer.Stop();

            try
            {
                await UpdateForecastsAsync();
            }
            finally
            {
                if (!IsDisposed)
                    _weatherTimer.Start();
            }
        }

        private readonly record struct WeatherLocation(
            string Name,
            double Latitude,
            double Longitude);

        private void LoadStatusConfiguration()
        {
            try
            {
                if (!File.Exists(StatusConfigPath))
                    return;

                foreach (string rawLine in File.ReadAllLines(StatusConfigPath))
                {
                    string line = rawLine.Trim();

                    if (string.IsNullOrWhiteSpace(line) ||
                        line.StartsWith("#"))
                    {
                        continue;
                    }

                    int separator = line.IndexOf('=');
                    if (separator <= 0)
                        continue;

                    string key = line[..separator]
                        .Trim()
                        .ToLowerInvariant();

                    string value = line[(separator + 1)..].Trim();

                    switch (key)
                    {
                        case "rycraft_host":
                            _rycraftHost = value;
                            break;

                        case "rycraft_port":
                            if (int.TryParse(value, out int port))
                                _rycraftPort = port;
                            break;

                        case "rycraft_local_host":
                            _rycraftLocalHost = value;
                            break;

                        case "rycraft_local_port":
                            if (int.TryParse(value, out int localPort))
                                _rycraftLocalPort = localPort;
                            break;

                        case "rycraft_rcon_host":
                            _rycraftRconHost = value;
                            break;

                        case "rycraft_rcon_port":
                            if (int.TryParse(value, out int rconPort))
                                _rycraftRconPort = rconPort;
                            break;

                        case "rycraft_rcon_password":
                            _rycraftRconPassword = value;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to load status.conf: {ex}");
            }
        }

        private static async Task<bool> IsTcpServiceOnlineAsync(
            string host,
            int port,
            int timeoutMilliseconds = 2500)
        {
            if (string.IsNullOrWhiteSpace(host))
                return false;

            try
            {
                using TcpClient client = new();
                using CancellationTokenSource timeout =
                    new(timeoutMilliseconds);

                await client.ConnectAsync(
                    host,
                    port,
                    timeout.Token);

                return client.Connected;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<bool> IsHostReachableByPingAsync(
            string host,
            int timeoutMilliseconds = CameraPingTimeoutMilliseconds)
        {
            try
            {
                using Ping ping = new();
                PingReply reply = await ping.SendPingAsync(
                    host,
                    timeoutMilliseconds);

                return reply.Status == IPStatus.Success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ping failed for {host}: {ex.Message}");
                return false;
            }
        }

        private async Task UpdateRycraftStatusAsync()
        {
            Task<bool> publicEndpointCheck =
                IsTcpServiceOnlineAsync(
                    _rycraftHost,
                    _rycraftPort);

            Task<MineStat?> localMinecraftCheck =
                QueryLocalRycraftAsync();

            Task<string?> rconPlayerListCheck =
                QueryRycraftPlayerNamesAsync();

            await Task.WhenAll(
                publicEndpointCheck,
                localMinecraftCheck,
                rconPlayerListCheck);

            bool publicOnline = publicEndpointCheck.Result;
            MineStat? localStatus = localMinecraftCheck.Result;
            string? rconPlayerNames = rconPlayerListCheck.Result;
            bool localOnline = localStatus?.ServerUp == true;

            _rycraftPlayerNames =
                rconPlayerNames ?? "Unavailable";

            if (rconPlayerNames != null)
            {
                Debug.WriteLine(
                    $"Rycraft RCON players: {_rycraftPlayerNames}");
            }

            if (localOnline)
            {
                if (!int.TryParse(
                    localStatus!.CurrentPlayers,
                    out _rycraftPlayersOnline))
                {
                    _rycraftPlayersOnline = -1;
                }

                if (!int.TryParse(
                    localStatus.MaximumPlayers,
                    out _rycraftPlayersMax))
                {
                    _rycraftPlayersMax = -1;
                }

                Debug.WriteLine(
                    $"Rycraft local Minecraft status: Online - " +
                    $"{_rycraftPlayersOnline}/{_rycraftPlayersMax} players");

                Debug.WriteLine($"Rycraft version: {localStatus.Version}");
                Debug.WriteLine($"Rycraft latency: {localStatus.Latency} ms");
                Debug.WriteLine($"Rycraft protocol: {localStatus.Protocol}");
            }
            else
            {
                _rycraftPlayersOnline = -1;
                _rycraftPlayersMax = -1;

                Debug.WriteLine(
                    "Rycraft local Minecraft status could not be retrieved.");
            }

            _rycraftStatus =
                publicOnline && localOnline
                    ? "Online"
                    : !publicOnline && localOnline
                        ? "Tunnel Offline"
                        : publicOnline
                            ? "Online"
                            : "Offline";

            Debug.WriteLine(
                $"Rycraft public endpoint: " +
                $"{(publicOnline ? "Online" : "Offline")}");

            Debug.WriteLine(
                $"Rycraft final status: {_rycraftStatus}");
        }

        private async Task<MineStat?> QueryLocalRycraftAsync()
        {
            if (string.IsNullOrWhiteSpace(_rycraftLocalHost))
            {
                Debug.WriteLine(
                    "Rycraft local host is not configured. " +
                    "Add rycraft_local_host to status.conf.");

                return null;
            }

            try
            {
                return await Task.Run(() =>
                    new MineStat(
                        _rycraftLocalHost,
                        (ushort)_rycraftLocalPort));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Rycraft local MineStat query failed: {ex}");

                return null;
            }
        }

        private async Task<string?> QueryRycraftPlayerNamesAsync()
        {
            string host = string.IsNullOrWhiteSpace(_rycraftRconHost)
                ? _rycraftLocalHost
                : _rycraftRconHost;

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(_rycraftRconPassword))
            {
                Debug.WriteLine(
                    "Rycraft RCON is not configured. " +
                    "Add rycraft_rcon_password " +
                    "(and optionally host/port) to status.conf.");

                return null;
            }

            try
            {
                using TcpClient client = new();
                using CancellationTokenSource timeout = new(3000);

                await client.ConnectAsync(
                    host,
                    _rycraftRconPort,
                    timeout.Token);

                using NetworkStream stream = client.GetStream();

                const int authRequestId = 1001;

                await SendRconPacketAsync(
                    stream,
                    authRequestId,
                    3,
                    _rycraftRconPassword,
                    timeout.Token);

                RconPacket authResponse =
                    await ReadRconPacketAsync(
                        stream,
                        timeout.Token);

                if (authResponse.RequestId == -1)
                {
                    Debug.WriteLine(
                        "Rycraft RCON authentication failed.");

                    return null;
                }

                const int commandRequestId = 1002;

                await SendRconPacketAsync(
                    stream,
                    commandRequestId,
                    2,
                    "list",
                    timeout.Token);

                RconPacket commandResponse =
                    await ReadRconPacketAsync(
                        stream,
                        timeout.Token);

                string response = commandResponse.Payload.Trim();

                Debug.WriteLine(
                    $"Rycraft RCON list response: {response}");

                return ParseRconPlayerNames(response);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Rycraft RCON query failed: {ex.Message}");

                return null;
            }
        }

        private static string ParseRconPlayerNames(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return "None";

            int colon = response.IndexOf(':');
            if (colon < 0)
                return "None";

            string names = response[(colon + 1)..].Trim();

            return string.IsNullOrWhiteSpace(names)
                ? "None"
                : names;
        }

        private static async Task SendRconPacketAsync(
            NetworkStream stream,
            int requestId,
            int type,
            string payload,
            CancellationToken cancellationToken)
        {
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
            int packetLength = 4 + 4 + payloadBytes.Length + 2;
            byte[] packet = new byte[4 + packetLength];

            BitConverter.GetBytes(packetLength).CopyTo(packet, 0);
            BitConverter.GetBytes(requestId).CopyTo(packet, 4);
            BitConverter.GetBytes(type).CopyTo(packet, 8);
            payloadBytes.CopyTo(packet, 12);

            packet[^2] = 0;
            packet[^1] = 0;

            await stream.WriteAsync(
                packet,
                cancellationToken);
        }

        private static async Task<RconPacket> ReadRconPacketAsync(
            NetworkStream stream,
            CancellationToken cancellationToken)
        {
            byte[] lengthBytes = await ReadExactlyAsync(
                stream,
                4,
                cancellationToken);

            int length = BitConverter.ToInt32(lengthBytes, 0);

            if (length < 10 || length > 1024 * 1024)
            {
                throw new IOException(
                    $"Invalid RCON packet length: {length}");
            }

            byte[] body = await ReadExactlyAsync(
                stream,
                length,
                cancellationToken);

            int requestId = BitConverter.ToInt32(body, 0);
            int type = BitConverter.ToInt32(body, 4);
            int payloadLength = Math.Max(0, length - 10);

            string payload = Encoding.UTF8.GetString(
                body,
                8,
                payloadLength);

            return new RconPacket(
                requestId,
                type,
                payload);
        }

        private static async Task<byte[]> ReadExactlyAsync(
            NetworkStream stream,
            int count,
            CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[count];
            int offset = 0;

            while (offset < count)
            {
                int read = await stream.ReadAsync(
                    buffer.AsMemory(offset, count - offset),
                    cancellationToken);

                if (read == 0)
                {
                    throw new IOException(
                        "RCON connection closed unexpectedly.");
                }

                offset += read;
            }

            return buffer;
        }

        private readonly record struct RconPacket(
            int RequestId,
            int Type,
            string Payload);

        private async Task UpdateStatusesAsync()
        {
            Task rycraftCheck = UpdateRycraftStatusAsync();
            Task<bool> cheddarCheck =
                IsHostReachableByPingAsync(CheddarCameraIp);
            Task<bool> denCheck =
                IsHostReachableByPingAsync(DenCameraIp);
            Task<bool> doorbellCheck =
                IsHostReachableByPingAsync(DoorbellCameraIp);

            await Task.WhenAll(
                rycraftCheck,
                cheddarCheck,
                denCheck,
                doorbellCheck);

            string cheddarStatus =
                cheddarCheck.Result ? "Online" : "Offline";
            string denStatus =
                denCheck.Result ? "Online" : "Offline";
            string doorbellStatus =
                doorbellCheck.Result ? "Online" : "Offline";

            _tapoStatus =
                $"Cheddar Camera (Backyard): {cheddarStatus} | " +
                $"Den Camera (Office): {denStatus} | " +
                $"Doorbell Camera (Front Door): {doorbellStatus}";

            Debug.WriteLine(
                $"Tapo Cheddar Camera ({CheddarCameraIp}): {cheddarStatus}");
            Debug.WriteLine(
                $"Tapo Den Camera ({DenCameraIp}): {denStatus}");
            Debug.WriteLine(
                $"Tapo Doorbell Camera ({DoorbellCameraIp}): {doorbellStatus}");
        }

        private async void StatusTimer_Tick(
            object? sender,
            EventArgs e)
        {
            _statusTimer.Stop();

            try
            {
                await UpdateStatusesAsync();
            }
            finally
            {
                if (!IsDisposed)
                    _statusTimer.Start();
            }
        }
    }
}
