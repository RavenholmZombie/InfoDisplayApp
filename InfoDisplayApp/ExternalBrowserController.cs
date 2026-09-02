using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InfoDisplayApp
{
    internal sealed class ExternalBrowserController : IDisposable
    {
        private readonly Form _owner;
        private readonly Control _viewport;
        private readonly BrowserWindow _philo;
        private readonly BrowserWindow _youtube;
        private bool _disposed;

        public ExternalBrowserController(Form owner, Control viewport)
        {
            _owner = owner;
            _viewport = viewport;

            string profileRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "InfoDisplayApp",
                "BrowserProfiles");

            _philo = new BrowserWindow(
                "Philo",
                "https://www.philo.com/",
                Path.Combine(profileRoot, "Philo"),
                9222,
                owner,
                viewport);

            _youtube = new BrowserWindow(
                "YouTube",
                "https://www.youtube.com/",
                Path.Combine(profileRoot, "YouTube"),
                9223,
                owner,
                viewport);

            _owner.Move += OwnerBoundsChanged;
            _owner.Resize += OwnerBoundsChanged;
            _owner.Activated += OwnerActivated;
            _viewport.Resize += OwnerBoundsChanged;
            _viewport.LocationChanged += OwnerBoundsChanged;
        }

        public async Task InitializeAsync()
        {
            ThrowIfDisposed();

            await _philo.StartAsync();
            await _youtube.StartAsync();

            await _philo.SetMutedAsync(false);
            await _youtube.SetMutedAsync(true);

            _youtube.Hide();
            _philo.Show();
        }

        public async Task ShowPhiloAsync()
        {
            ThrowIfDisposed();

            await _youtube.SetMutedAsync(true);
            _youtube.Hide();

            _philo.Show();
            await _philo.SetMutedAsync(false);
        }

        public async Task ShowYouTubeAsync()
        {
            ThrowIfDisposed();

            await _philo.SetMutedAsync(true);
            _philo.Hide();

            _youtube.Show();
            await _youtube.SetMutedAsync(false);
        }

        public async Task HideAllAsync()
        {
            if (_disposed)
                return;

            await Task.WhenAll(
                _philo.SetMutedAsync(true),
                _youtube.SetMutedAsync(true));

            _philo.Hide();
            _youtube.Hide();
        }

        public void RepositionVisibleWindows()
        {
            if (_disposed)
                return;

            _philo.PositionIfVisible();
            _youtube.PositionIfVisible();
        }

        private void OwnerBoundsChanged(object? sender, EventArgs e) =>
            RepositionVisibleWindows();

        private void OwnerActivated(object? sender, EventArgs e)
        {
            RepositionVisibleWindows();
            _philo.RefreshZOrderIfVisible();
            _youtube.RefreshZOrderIfVisible();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ExternalBrowserController));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _owner.Move -= OwnerBoundsChanged;
            _owner.Resize -= OwnerBoundsChanged;
            _owner.Activated -= OwnerActivated;
            _viewport.Resize -= OwnerBoundsChanged;
            _viewport.LocationChanged -= OwnerBoundsChanged;

            _philo.Dispose();
            _youtube.Dispose();
        }

        private sealed class BrowserWindow : IDisposable
        {
            private readonly string _name;
            private readonly string _url;
            private readonly string _profileDirectory;
            private readonly int _debugPort;
            private readonly Form _owner;
            private readonly Control _viewport;

            private Process? _launcherProcess;
            private int? _windowProcessId;
            private IntPtr _windowHandle;
            private bool _visible;
            private bool _disposed;

            private static readonly System.Net.Http.HttpClient DevToolsHttp = new()
            {
                Timeout = TimeSpan.FromSeconds(3)
            };

            private const int GwlStyle = -16;
            private const int GwlExStyle = -20;
            private const int GwlpHwndParent = -8;

            private const long WsCaption = 0x00C00000L;
            private const long WsThickFrame = 0x00040000L;
            private const long WsMinimizeBox = 0x00020000L;
            private const long WsMaximizeBox = 0x00010000L;
            private const long WsSysMenu = 0x00080000L;
            private const long WsExToolWindow = 0x00000080L;
            private const long WsExAppWindow = 0x00040000L;

            private const uint SwpNoActivate = 0x0010;
            private const uint SwpFrameChanged = 0x0020;
            private const uint SwpShowWindow = 0x0040;
            private const uint SwpNoMove = 0x0002;
            private const uint SwpNoSize = 0x0001;
            private const int SwHide = 0;
            private const int SwShowNoActivate = 8;
            private const uint WmClose = 0x0010;

            // frmMain itself is TopMost. HWND_TOP only moves a window within its
            // current z-order band, so Edge could remain underneath frmMain.
            // Make only the active browser window topmost so it can occupy pnlTV.
            private static readonly IntPtr HwndTopMost = new(-1);

            public BrowserWindow(
                string name,
                string url,
                string profileDirectory,
                int debugPort,
                Form owner,
                Control viewport)
            {
                _name = name;
                _url = url;
                _profileDirectory = profileDirectory;
                _debugPort = debugPort;
                _owner = owner;
                _viewport = viewport;
            }

            public async Task StartAsync()
            {
                if (_disposed)
                    return;

                Directory.CreateDirectory(_profileDirectory);

                string edgePath = FindEdgeExecutable();
                HashSet<IntPtr> windowsBeforeLaunch = SnapshotVisibleEdgeWindows();

                string arguments =
                    $"--app=\"{_url}\" " +
                    $"--user-data-dir=\"{_profileDirectory}\" " +
                    "--remote-debugging-address=127.0.0.1 " +
                    $"--remote-debugging-port={_debugPort} " +
                    "--no-first-run " +
                    "--no-default-browser-check " +
                    "--disable-session-crashed-bubble " +
                    "--disable-features=msEdgeSidebarV2";

                ProcessStartInfo startInfo = new()
                {
                    FileName = edgePath,
                    Arguments = arguments,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(edgePath) ?? AppContext.BaseDirectory
                };

                _launcherProcess = Process.Start(startInfo)
                    ?? throw new InvalidOperationException($"Unable to start {_name} browser.");

                _windowHandle = await WaitForNewEdgeWindowAsync(
                    windowsBeforeLaunch,
                    _name,
                    TimeSpan.FromSeconds(20));

                if (_windowHandle == IntPtr.Zero)
                {
                    throw new InvalidOperationException(
                        $"{_name} browser was launched, but InfoDisplay could not locate its Edge app window.");
                }

                GetWindowThreadProcessId(_windowHandle, out uint actualProcessId);
                if (actualProcessId != 0)
                    _windowProcessId = (int)actualProcessId;

                ConfigureWindow();

                Debug.WriteLine(
                    $"{_name}: external Edge viewer started. " +
                    $"Window PID={_windowProcessId?.ToString() ?? "unknown"}, " +
                    $"DevTools port={_debugPort}.");
            }

            public async Task SetMutedAsync(bool muted)
            {
                if (_disposed || _windowHandle == IntPtr.Zero || !IsWindow(_windowHandle))
                    return;

                string value = muted ? "true" : "false";
                string expression =
                    $"document.querySelectorAll('video,audio').forEach(m => m.muted = {value});";

                for (int attempt = 0; attempt < 4; attempt++)
                {
                    try
                    {
                        if (await EvaluateJavaScriptAsync(expression))
                            return;
                    }
                    catch (Exception ex)
                    {
                        if (attempt == 3)
                            Debug.WriteLine($"{_name}: unable to update mute state: {ex.Message}");
                    }

                    await Task.Delay(350);
                }
            }

            public void Show()
            {
                if (_windowHandle == IntPtr.Zero || _disposed || !IsWindow(_windowHandle))
                    return;

                ShowWindow(_windowHandle, SwShowNoActivate);
                _visible = true;
                Position();
                RefreshZOrderIfVisible();
            }

            public void Hide()
            {
                if (_windowHandle == IntPtr.Zero || _disposed || !IsWindow(_windowHandle))
                    return;

                ShowWindow(_windowHandle, SwHide);
                _visible = false;
            }

            public void PositionIfVisible()
            {
                if (_visible)
                    Position();
            }

            public void RefreshZOrderIfVisible()
            {
                if (!_visible || _windowHandle == IntPtr.Zero || !IsWindow(_windowHandle))
                    return;

                SetWindowPos(
                    _windowHandle,
                    HwndTopMost,
                    0,
                    0,
                    0,
                    0,
                    SwpNoMove | SwpNoSize | SwpNoActivate);
            }

            public void Position()
            {
                if (_windowHandle == IntPtr.Zero ||
                    _disposed ||
                    !IsWindow(_windowHandle) ||
                    !_viewport.IsHandleCreated)
                {
                    return;
                }

                Point screenLocation = _viewport.PointToScreen(Point.Empty);
                Size size = _viewport.ClientSize;

                SetWindowPos(
                    _windowHandle,
                    HwndTopMost,
                    screenLocation.X,
                    screenLocation.Y,
                    Math.Max(size.Width, 1),
                    Math.Max(size.Height, 1),
                    SwpNoActivate | SwpShowWindow);
            }

            private void ConfigureWindow()
            {
                long style = GetWindowLongPtr(_windowHandle, GwlStyle).ToInt64();
                style &= ~(WsCaption | WsThickFrame | WsMinimizeBox | WsMaximizeBox | WsSysMenu);
                SetWindowLongPtr(_windowHandle, GwlStyle, new IntPtr(style));

                long exStyle = GetWindowLongPtr(_windowHandle, GwlExStyle).ToInt64();
                exStyle |= WsExToolWindow;
                exStyle &= ~WsExAppWindow;
                SetWindowLongPtr(_windowHandle, GwlExStyle, new IntPtr(exStyle));

                SetWindowLongPtr(_windowHandle, GwlpHwndParent, _owner.Handle);

                SetWindowPos(
                    _windowHandle,
                    HwndTopMost,
                    0,
                    0,
                    0,
                    0,
                    SwpNoMove | SwpNoSize | SwpNoActivate | SwpFrameChanged);
            }

            private async Task<bool> EvaluateJavaScriptAsync(string expression)
            {
                string endpoint = $"http://127.0.0.1:{_debugPort}/json";
                string targetsJson = await DevToolsHttp.GetStringAsync(endpoint);

                using JsonDocument targets = JsonDocument.Parse(targetsJson);

                JsonElement? target = targets.RootElement
                    .EnumerateArray()
                    .Where(item =>
                        item.TryGetProperty("type", out JsonElement type) &&
                        type.GetString() == "page")
                    .OrderByDescending(item =>
                        item.TryGetProperty("url", out JsonElement url) &&
                        url.GetString()?.StartsWith(_url, StringComparison.OrdinalIgnoreCase) == true)
                    .Cast<JsonElement?>()
                    .FirstOrDefault();

                if (target == null ||
                    !target.Value.TryGetProperty("webSocketDebuggerUrl", out JsonElement websocketProperty))
                {
                    return false;
                }

                string? websocketUrl = websocketProperty.GetString();
                if (string.IsNullOrWhiteSpace(websocketUrl))
                    return false;

                using ClientWebSocket socket = new();
                using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(3));

                await socket.ConnectAsync(new Uri(websocketUrl), timeout.Token);

                string command = JsonSerializer.Serialize(new
                {
                    id = 1,
                    method = "Runtime.evaluate",
                    @params = new
                    {
                        expression,
                        returnByValue = true
                    }
                });

                byte[] payload = Encoding.UTF8.GetBytes(command);
                await socket.SendAsync(
                    payload,
                    WebSocketMessageType.Text,
                    true,
                    timeout.Token);

                byte[] responseBuffer = new byte[4096];
                await socket.ReceiveAsync(responseBuffer, timeout.Token);
                return true;
            }

            private static async Task<IntPtr> WaitForNewEdgeWindowAsync(
                HashSet<IntPtr> windowsBeforeLaunch,
                string preferredTitle,
                TimeSpan timeout)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                while (stopwatch.Elapsed < timeout)
                {
                    List<EdgeWindowInfo> candidates = GetVisibleEdgeWindows()
                        .Where(window => !windowsBeforeLaunch.Contains(window.Handle))
                        .ToList();

                    EdgeWindowInfo preferred = candidates.FirstOrDefault(window =>
                        window.Title.Contains(preferredTitle, StringComparison.OrdinalIgnoreCase));

                    if (preferred.Handle != IntPtr.Zero)
                        return preferred.Handle;

                    if (candidates.Count == 1)
                        return candidates[0].Handle;

                    EdgeWindowInfo fallback = GetVisibleEdgeWindows().FirstOrDefault(window =>
                        window.Title.Contains(preferredTitle, StringComparison.OrdinalIgnoreCase));

                    if (fallback.Handle != IntPtr.Zero)
                        return fallback.Handle;

                    await Task.Delay(150);
                }

                return IntPtr.Zero;
            }

            private static HashSet<IntPtr> SnapshotVisibleEdgeWindows() =>
                GetVisibleEdgeWindows().Select(window => window.Handle).ToHashSet();

            private static List<EdgeWindowInfo> GetVisibleEdgeWindows()
            {
                List<EdgeWindowInfo> windows = new();

                EnumWindows((handle, _) =>
                {
                    if (!IsWindowVisible(handle))
                        return true;

                    GetWindowThreadProcessId(handle, out uint processId);
                    if (processId == 0 || !IsEdgeProcess((int)processId))
                        return true;

                    windows.Add(new EdgeWindowInfo(
                        handle,
                        (int)processId,
                        GetWindowTitle(handle)));

                    return true;
                }, IntPtr.Zero);

                return windows;
            }

            private static bool IsEdgeProcess(int processId)
            {
                try
                {
                    using Process process = Process.GetProcessById(processId);
                    return process.ProcessName.Equals("msedge", StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            }

            private static string GetWindowTitle(IntPtr handle)
            {
                int length = GetWindowTextLength(handle);
                if (length <= 0)
                    return string.Empty;

                StringBuilder title = new(length + 1);
                GetWindowText(handle, title, title.Capacity);
                return title.ToString();
            }

            private static string FindEdgeExecutable()
            {
                string[] candidates =
                {
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                        "Microsoft", "Edge", "Application", "msedge.exe"),
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                        "Microsoft", "Edge", "Application", "msedge.exe")
                };

                string? found = candidates.FirstOrDefault(File.Exists);
                return found ?? "msedge.exe";
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;

                try
                {
                    if (_windowHandle != IntPtr.Zero && IsWindow(_windowHandle))
                        PostMessage(_windowHandle, WmClose, IntPtr.Zero, IntPtr.Zero);

                    if (_windowProcessId.HasValue)
                    {
                        try
                        {
                            using Process actualProcess = Process.GetProcessById(_windowProcessId.Value);
                            if (!actualProcess.WaitForExit(1500))
                                actualProcess.Kill(entireProcessTree: true);
                        }
                        catch (ArgumentException)
                        {
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"{_name}: browser shutdown issue: {ex.Message}");
                }
                finally
                {
                    _launcherProcess?.Dispose();
                    _launcherProcess = null;
                    _windowProcessId = null;
                    _windowHandle = IntPtr.Zero;
                }
            }

            private readonly record struct EdgeWindowInfo(
                IntPtr Handle,
                int ProcessId,
                string Title);

            private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

            [DllImport("user32.dll")]
            private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

            [DllImport("user32.dll")]
            private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

            [DllImport("user32.dll")]
            private static extern bool IsWindowVisible(IntPtr hWnd);

            [DllImport("user32.dll")]
            private static extern bool IsWindow(IntPtr hWnd);

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            private static extern int GetWindowTextLength(IntPtr hWnd);

            [DllImport("user32.dll")]
            private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

            [DllImport("user32.dll")]
            private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool SetWindowPos(
                IntPtr hWnd,
                IntPtr hWndInsertAfter,
                int x,
                int y,
                int width,
                int height,
                uint flags);

            [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
            private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int index);

            [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
            private static extern IntPtr GetWindowLong32(IntPtr hWnd, int index);

            private static IntPtr GetWindowLongPtr(IntPtr hWnd, int index) =>
                IntPtr.Size == 8
                    ? GetWindowLongPtr64(hWnd, index)
                    : GetWindowLong32(hWnd, index);

            [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
            private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int index, IntPtr newLong);

            [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
            private static extern IntPtr SetWindowLong32(IntPtr hWnd, int index, IntPtr newLong);

            private static IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr newLong) =>
                IntPtr.Size == 8
                    ? SetWindowLongPtr64(hWnd, index, newLong)
                    : SetWindowLong32(hWnd, index, newLong);
        }
    }
}
