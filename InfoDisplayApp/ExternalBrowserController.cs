using System;
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
    /// <summary>
    /// Hosts Philo and YouTube in normal Edge app windows instead of WebView2.
    /// The browser windows remain top-level windows owned by InfoDisplayApp and
    /// are positioned directly over pnlTV. Playback is controlled through the
    /// Chromium DevTools protocol exposed on localhost.
    /// </summary>
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

            // Launch both services up front. With separate profiles, Philo and
            // YouTube keep independent cookies/login state and renderer processes.
            await Task.WhenAll(
                _philo.StartAsync(),
                _youtube.StartAsync());

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

            _philo.Position();
            _philo.Show();
            await _philo.SetMutedAsync(false);
        }

        public async Task ShowYouTubeAsync()
        {
            ThrowIfDisposed();

            await _philo.SetMutedAsync(true);
            _philo.Hide();

            _youtube.Position();
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
            // Owned browser windows normally stay above the owner, but nudging
            // their z-order here keeps them over pnlTV after task switching.
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
            private Process? _process;
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
            private const int SwHide = 0;
            private const int SwShowNoActivate = 8;

            private static readonly IntPtr HwndTop = new(0);

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
                string arguments =
                    $"--app=\"{_url}\" " +
                    $"--user-data-dir=\"{_profileDirectory}\" " +
                    $"--remote-debugging-address=127.0.0.1 " +
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

                _process = Process.Start(startInfo)
                    ?? throw new InvalidOperationException($"Unable to start {_name} browser.");

                _windowHandle = await WaitForWindowAsync(_process, TimeSpan.FromSeconds(15));

                if (_windowHandle == IntPtr.Zero)
                {
                    throw new InvalidOperationException(
                        $"{_name} browser started but no visible app window was found.");
                }

                ConfigureWindow();
                Position();

                Debug.WriteLine($"{_name}: external Edge viewer started on DevTools port {_debugPort}.");
            }

            public async Task SetMutedAsync(bool muted)
            {
                if (_disposed || _process == null || _process.HasExited)
                    return;

                string value = muted ? "true" : "false";
                string expression =
                    $"document.querySelectorAll('video,audio').forEach(m => m.muted = {value});";

                // Chromium pages may not expose the media element immediately after
                // startup/navigation. A few short attempts make switching reliable.
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
                if (_windowHandle == IntPtr.Zero || _disposed)
                    return;

                Position();
                ShowWindow(_windowHandle, SwShowNoActivate);
                _visible = true;
                RefreshZOrderIfVisible();
            }

            public void Hide()
            {
                if (_windowHandle == IntPtr.Zero || _disposed)
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
                if (!_visible || _windowHandle == IntPtr.Zero)
                    return;

                SetWindowPos(
                    _windowHandle,
                    HwndTop,
                    0,
                    0,
                    0,
                    0,
                    0x0001 | 0x0002 | SwpNoActivate);
            }

            public void Position()
            {
                if (_windowHandle == IntPtr.Zero || _disposed || !_viewport.IsHandleCreated)
                    return;

                Point screenLocation = _viewport.PointToScreen(Point.Empty);
                Size size = _viewport.ClientSize;

                SetWindowPos(
                    _windowHandle,
                    HwndTop,
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

                // Make the browser a top-level owned window, not a child window.
                // This keeps Chromium happy while tying its z-order/minimize state
                // to InfoDisplayApp.
                SetWindowLongPtr(_windowHandle, GwlpHwndParent, _owner.Handle);

                SetWindowPos(
                    _windowHandle,
                    HwndTop,
                    0,
                    0,
                    0,
                    0,
                    0x0001 | 0x0002 | SwpNoActivate | SwpFrameChanged);
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

                // Receiving the acknowledgement ensures Chromium accepted the
                // command before the short-lived DevTools connection is closed.
                byte[] responseBuffer = new byte[4096];
                await socket.ReceiveAsync(responseBuffer, timeout.Token);
                return true;
            }

            private static async Task<IntPtr> WaitForWindowAsync(
                Process process,
                TimeSpan timeout)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                while (stopwatch.Elapsed < timeout)
                {
                    if (process.HasExited)
                        return IntPtr.Zero;

                    process.Refresh();
                    if (process.MainWindowHandle != IntPtr.Zero)
                        return process.MainWindowHandle;

                    IntPtr enumerated = FindTopLevelWindowForProcess(process.Id);
                    if (enumerated != IntPtr.Zero)
                        return enumerated;

                    await Task.Delay(150);
                }

                return IntPtr.Zero;
            }

            private static IntPtr FindTopLevelWindowForProcess(int processId)
            {
                IntPtr result = IntPtr.Zero;

                EnumWindows((handle, _) =>
                {
                    GetWindowThreadProcessId(handle, out uint windowProcessId);

                    if (windowProcessId == processId && IsWindowVisible(handle))
                    {
                        result = handle;
                        return false;
                    }

                    return true;
                }, IntPtr.Zero);

                return result;
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
                    Hide();

                    if (_process != null && !_process.HasExited)
                    {
                        _process.CloseMainWindow();

                        if (!_process.WaitForExit(1500))
                            _process.Kill(entireProcessTree: true);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"{_name}: browser shutdown issue: {ex.Message}");
                }
                finally
                {
                    _process?.Dispose();
                    _process = null;
                    _windowHandle = IntPtr.Zero;
                }
            }

            private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

            [DllImport("user32.dll")]
            private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

            [DllImport("user32.dll")]
            private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

            [DllImport("user32.dll")]
            private static extern bool IsWindowVisible(IntPtr hWnd);

            [DllImport("user32.dll")]
            private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

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
