using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using MineStatLib;

namespace InfoDisplayApp.Properties
{
    public partial class ctrlTicker : UserControl
    {
        private readonly System.Windows.Forms.Timer _scrollTimer;
        private readonly System.Windows.Forms.Timer _reloadTimer;
        private readonly System.Windows.Forms.Timer _statusTimer;

        private string _rycraftStatus = "Checking...";
        private int _rycraftPlayersOnline = 0;
        private int _rycraftPlayersMax = 0;
        private string _tapoStatus = "Checking...";
        private string _rycraftHost = "";
        private int _rycraftPort = 25565;
        private string _rycraftLocalHost = "";
        private int _rycraftLocalPort = 25565;
        private string _rycraftRconHost = "";
        private int _rycraftRconPort = 25575;
        private string _rycraftRconPassword = "";
        private string _rycraftPlayerNames = "Unavailable";
        private string _tapoHost = "";
        private const int TapoRtspPort = 554;
        private string StatusConfigPath => Path.Combine(AppContext.BaseDirectory, "status.conf");
        private string CameraConfigPath => Path.Combine(AppContext.BaseDirectory, "camera.conf");
        private readonly List<string> _messages = new();
        private int _currentMessageIndex;
        private readonly Stopwatch _scrollClock = new();
        private double _lastScrollSeconds;
        private double _scrollX;
        private float _messageWidth;
        private string _renderedMessage = "";
        private const double ScrollPixelsPerSecond = 625.0;
        private const int MessageGap = 50;
        private string TickerPath => Path.Combine(AppContext.BaseDirectory, "ticker.txt");

        public ctrlTicker()
        {
            InitializeComponent();
            lblTextTicker.Visible = false;
            panel1.Paint += panel1_Paint;
            panel1.Resize += panel1_Resize;
            _scrollTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _scrollTimer.Tick += ScrollTimer_Tick;
            _reloadTimer = new System.Windows.Forms.Timer { Interval = 10_000 };
            _reloadTimer.Tick += ReloadTimer_Tick;
            _statusTimer = new System.Windows.Forms.Timer { Interval = 30_000 };
            _statusTimer.Tick += StatusTimer_Tick;
            Load += ctrlTicker_Load;
            Disposed += ctrlTicker_Disposed;
        }

        private async void ctrlTicker_Load(object? sender, EventArgs e)
        {
            LoadStatusConfiguration();
            await UpdateStatusesAsync();
            LoadTickerMessages();
            if (_messages.Count > 0)
            {
                ShowCurrentMessage();
                _scrollTimer.Start();
            }
            _reloadTimer.Start();
            _statusTimer.Start();
        }

        private void LoadTickerMessages()
        {
            try
            {
                if (!File.Exists(TickerPath))
                {
                    Debug.WriteLine($"Ticker file not found: {TickerPath}");
                    _messages.Clear();
                    _renderedMessage = "";
                    panel1.Invalidate();
                    return;
                }
                List<string> newMessages = File.ReadAllLines(TickerPath)
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                    .ToList();
                if (newMessages.Count == 0)
                {
                    _messages.Clear();
                    _renderedMessage = "";
                    panel1.Invalidate();
                    return;
                }
                if (_messages.SequenceEqual(newMessages)) return;
                _messages.Clear();
                _messages.AddRange(newMessages);
                _currentMessageIndex = 0;
                ShowCurrentMessage();
            }
            catch (Exception ex) { Debug.WriteLine($"Unable to load ticker.txt: {ex}"); }
        }

        private void ShowCurrentMessage()
        {
            if (_messages.Count == 0)
            {
                _renderedMessage = "";
                panel1.Invalidate();
                return;
            }
            if (_currentMessageIndex >= _messages.Count) _currentMessageIndex = 0;
            _renderedMessage = _messages[_currentMessageIndex]
                .Replace("{RYCRAFT_STATUS}", _rycraftStatus, StringComparison.OrdinalIgnoreCase)
                .Replace("{RYCRAFT_PLAYERS}", _rycraftPlayersOnline >= 0 && _rycraftPlayersMax >= 0 ? $"{_rycraftPlayersOnline}/{_rycraftPlayersMax}" : "Unavailable", StringComparison.OrdinalIgnoreCase)
                .Replace("{RYCRAFT_ONLINE_PLAYERS}", _rycraftPlayersOnline.ToString(), StringComparison.OrdinalIgnoreCase)
                .Replace("{RYCRAFT_MAX_PLAYERS}", _rycraftPlayersMax.ToString(), StringComparison.OrdinalIgnoreCase)
                .Replace("{RYCRAFT_PLAYER_NAMES}", _rycraftPlayerNames, StringComparison.OrdinalIgnoreCase)
                .Replace("{TAPO_STATUS}", _tapoStatus, StringComparison.OrdinalIgnoreCase);
            using (Graphics graphics = panel1.CreateGraphics())
            {
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                _messageWidth = graphics.MeasureString(_renderedMessage, lblTextTicker.Font, int.MaxValue, StringFormat.GenericTypographic).Width + 10f;
            }
            _scrollX = panel1.ClientSize.Width + MessageGap;
            _scrollClock.Restart();
            _lastScrollSeconds = 0;
            panel1.Invalidate();
        }

        private void ScrollTimer_Tick(object? sender, EventArgs e)
        {
            if (_messages.Count == 0) return;
            double nowSeconds = _scrollClock.Elapsed.TotalSeconds;
            double elapsedSeconds = Math.Min(nowSeconds - _lastScrollSeconds, 0.100);
            _lastScrollSeconds = nowSeconds;
            _scrollX -= ScrollPixelsPerSecond * elapsedSeconds;
            panel1.Invalidate();
            if (_scrollX + _messageWidth < 0)
            {
                _currentMessageIndex++;
                if (_currentMessageIndex >= _messages.Count) _currentMessageIndex = 0;
                ShowCurrentMessage();
            }
        }

        private void panel1_Paint(object? sender, PaintEventArgs e)
        {
            if (string.IsNullOrEmpty(_renderedMessage)) return;
            e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            using SolidBrush brush = new(lblTextTicker.ForeColor);
            using StringFormat format = new(StringFormat.GenericTypographic)
            {
                LineAlignment = StringAlignment.Center,
                Alignment = StringAlignment.Near,
                FormatFlags = StringFormatFlags.NoWrap
            };
            e.Graphics.DrawString(_renderedMessage, lblTextTicker.Font, brush,
                new RectangleF((float)_scrollX, 0, Math.Max(_messageWidth, 1f), panel1.ClientSize.Height), format);
        }

        private void panel1_Resize(object? sender, EventArgs e) => panel1.Invalidate();

        private void ReloadTimer_Tick(object? sender, EventArgs e)
        {
            LoadTickerMessages();
            if (_messages.Count > 0 && !_scrollTimer.Enabled)
            {
                ShowCurrentMessage();
                _scrollTimer.Start();
            }
            if (_messages.Count == 0) _scrollTimer.Stop();
        }

        private void ctrlTicker_Disposed(object? sender, EventArgs e)
        {
            _scrollTimer.Stop(); _reloadTimer.Stop(); _statusTimer.Stop();
            _scrollTimer.Dispose(); _reloadTimer.Dispose(); _statusTimer.Dispose(); _scrollClock.Stop();
        }

        private void LoadStatusConfiguration()
        {
            try
            {
                if (File.Exists(StatusConfigPath))
                {
                    foreach (string rawLine in File.ReadAllLines(StatusConfigPath))
                    {
                        string line = rawLine.Trim();
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                        int separator = line.IndexOf('=');
                        if (separator <= 0) continue;
                        string key = line[..separator].Trim().ToLowerInvariant();
                        string value = line[(separator + 1)..].Trim();
                        switch (key)
                        {
                            case "rycraft_host": _rycraftHost = value; break;
                            case "rycraft_port": if (int.TryParse(value, out int port)) _rycraftPort = port; break;
                            case "rycraft_local_host": _rycraftLocalHost = value; break;
                            case "rycraft_local_port": if (int.TryParse(value, out int localPort)) _rycraftLocalPort = localPort; break;
                            case "rycraft_rcon_host": _rycraftRconHost = value; break;
                            case "rycraft_rcon_port": if (int.TryParse(value, out int rconPort)) _rycraftRconPort = rconPort; break;
                            case "rycraft_rcon_password": _rycraftRconPassword = value; break;
                        }
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"Unable to load status.conf: {ex}"); }
            try
            {
                if (File.Exists(CameraConfigPath))
                {
                    foreach (string rawLine in File.ReadAllLines(CameraConfigPath))
                    {
                        string line = rawLine.Trim();
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                        int separator = line.IndexOf('=');
                        if (separator <= 0) continue;
                        string key = line[..separator].Trim().ToLowerInvariant();
                        string value = line[(separator + 1)..].Trim();
                        if (key == "ip") { _tapoHost = value; break; }
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"Unable to read camera address: {ex}"); }
        }

        private static async Task<bool> IsTcpServiceOnlineAsync(string host, int port, int timeoutMilliseconds = 2500)
        {
            if (string.IsNullOrWhiteSpace(host)) return false;
            try
            {
                using TcpClient client = new();
                using CancellationTokenSource timeout = new(timeoutMilliseconds);
                await client.ConnectAsync(host, port, timeout.Token);
                return client.Connected;
            }
            catch { return false; }
        }

        private async Task UpdateRycraftStatusAsync()
        {
            Task<bool> publicEndpointCheck = IsTcpServiceOnlineAsync(_rycraftHost, _rycraftPort);
            Task<MineStat?> localMinecraftCheck = QueryLocalRycraftAsync();
            Task<string?> rconPlayerListCheck = QueryRycraftPlayerNamesAsync();
            await Task.WhenAll(publicEndpointCheck, localMinecraftCheck, rconPlayerListCheck);
            bool publicOnline = publicEndpointCheck.Result;
            MineStat? localStatus = localMinecraftCheck.Result;
            string? rconPlayerNames = rconPlayerListCheck.Result;
            bool localOnline = localStatus?.ServerUp == true;
            _rycraftPlayerNames = rconPlayerNames ?? "Unavailable";
            if (rconPlayerNames != null) Debug.WriteLine($"Rycraft RCON players: {_rycraftPlayerNames}");
            if (localOnline)
            {
                if (!int.TryParse(localStatus!.CurrentPlayers, out _rycraftPlayersOnline)) _rycraftPlayersOnline = -1;
                if (!int.TryParse(localStatus.MaximumPlayers, out _rycraftPlayersMax)) _rycraftPlayersMax = -1;
                Debug.WriteLine($"Rycraft local Minecraft status: Online - {_rycraftPlayersOnline}/{_rycraftPlayersMax} players");
                Debug.WriteLine($"Rycraft version: {localStatus.Version}");
                Debug.WriteLine($"Rycraft latency: {localStatus.Latency} ms");
                Debug.WriteLine($"Rycraft protocol: {localStatus.Protocol}");
            }
            else
            {
                _rycraftPlayersOnline = -1;
                _rycraftPlayersMax = -1;
                Debug.WriteLine("Rycraft local Minecraft status could not be retrieved.");
            }
            _rycraftStatus = publicOnline && localOnline ? "Online" : !publicOnline && localOnline ? "Tunnel Offline" : publicOnline ? "Online" : "Offline";
            Debug.WriteLine($"Rycraft public endpoint: {(publicOnline ? "Online" : "Offline")}");
            Debug.WriteLine($"Rycraft final status: {_rycraftStatus}");
        }

        private async Task<MineStat?> QueryLocalRycraftAsync()
        {
            if (string.IsNullOrWhiteSpace(_rycraftLocalHost))
            {
                Debug.WriteLine("Rycraft local host is not configured. Add rycraft_local_host to status.conf.");
                return null;
            }
            try { return await Task.Run(() => new MineStat(_rycraftLocalHost, (ushort)_rycraftLocalPort)); }
            catch (Exception ex) { Debug.WriteLine($"Rycraft local MineStat query failed: {ex}"); return null; }
        }

        private async Task<string?> QueryRycraftPlayerNamesAsync()
        {
            string host = string.IsNullOrWhiteSpace(_rycraftRconHost) ? _rycraftLocalHost : _rycraftRconHost;
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(_rycraftRconPassword))
            {
                Debug.WriteLine("Rycraft RCON is not configured. Add rycraft_rcon_password (and optionally host/port) to status.conf.");
                return null;
            }
            try
            {
                using TcpClient client = new();
                using CancellationTokenSource timeout = new(3000);
                await client.ConnectAsync(host, _rycraftRconPort, timeout.Token);
                using NetworkStream stream = client.GetStream();
                const int authRequestId = 1001;
                await SendRconPacketAsync(stream, authRequestId, 3, _rycraftRconPassword, timeout.Token);
                RconPacket authResponse = await ReadRconPacketAsync(stream, timeout.Token);
                if (authResponse.RequestId == -1) { Debug.WriteLine("Rycraft RCON authentication failed."); return null; }
                const int commandRequestId = 1002;
                await SendRconPacketAsync(stream, commandRequestId, 2, "list", timeout.Token);
                RconPacket commandResponse = await ReadRconPacketAsync(stream, timeout.Token);
                string response = commandResponse.Payload.Trim();
                Debug.WriteLine($"Rycraft RCON list response: {response}");
                return ParseRconPlayerNames(response);
            }
            catch (Exception ex) { Debug.WriteLine($"Rycraft RCON query failed: {ex.Message}"); return null; }
        }

        private static string ParseRconPlayerNames(string response)
        {
            if (string.IsNullOrWhiteSpace(response)) return "None";
            int colon = response.IndexOf(':');
            if (colon < 0) return "None";
            string names = response[(colon + 1)..].Trim();
            return string.IsNullOrWhiteSpace(names) ? "None" : names;
        }

        private static async Task SendRconPacketAsync(NetworkStream stream, int requestId, int type, string payload, CancellationToken cancellationToken)
        {
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
            int packetLength = 4 + 4 + payloadBytes.Length + 2;
            byte[] packet = new byte[4 + packetLength];
            BitConverter.GetBytes(packetLength).CopyTo(packet, 0);
            BitConverter.GetBytes(requestId).CopyTo(packet, 4);
            BitConverter.GetBytes(type).CopyTo(packet, 8);
            payloadBytes.CopyTo(packet, 12);
            packet[^2] = 0; packet[^1] = 0;
            await stream.WriteAsync(packet, cancellationToken);
        }

        private static async Task<RconPacket> ReadRconPacketAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            byte[] lengthBytes = await ReadExactlyAsync(stream, 4, cancellationToken);
            int length = BitConverter.ToInt32(lengthBytes, 0);
            if (length < 10 || length > 1024 * 1024) throw new IOException($"Invalid RCON packet length: {length}");
            byte[] body = await ReadExactlyAsync(stream, length, cancellationToken);
            int requestId = BitConverter.ToInt32(body, 0);
            int type = BitConverter.ToInt32(body, 4);
            int payloadLength = Math.Max(0, length - 10);
            string payload = Encoding.UTF8.GetString(body, 8, payloadLength);
            return new RconPacket(requestId, type, payload);
        }

        private static async Task<byte[]> ReadExactlyAsync(NetworkStream stream, int count, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), cancellationToken);
                if (read == 0) throw new IOException("RCON connection closed unexpectedly.");
                offset += read;
            }
            return buffer;
        }

        private readonly record struct RconPacket(int RequestId, int Type, string Payload);

        private async Task UpdateStatusesAsync()
        {
            Task rycraftCheck = UpdateRycraftStatusAsync();
            Task<bool> tapoCheck = IsTcpServiceOnlineAsync(_tapoHost, TapoRtspPort);
            await Task.WhenAll(rycraftCheck, tapoCheck);
            _tapoStatus = tapoCheck.Result ? "Online" : "Offline";
            Debug.WriteLine($"Tapo: {_tapoStatus}");
        }

        private async void StatusTimer_Tick(object? sender, EventArgs e)
        {
            _statusTimer.Stop();
            try { await UpdateStatusesAsync(); }
            finally { _statusTimer.Start(); }
        }
    }
}
