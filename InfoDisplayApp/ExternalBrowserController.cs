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
        private const string PhiloUrl = "https://www.philo.com/";
        private const string YouTubeUrl = "https://www.youtube.com/";

        private readonly BrowserWindow _browser;
        private readonly System.Windows.Forms.Timer _zOrderTimer;
        private bool _disposed;

        public ExternalBrowserController(Form owner, Control viewport)
        {
            // Reuse the existing Philo profile so the login/cookies already created
            // while testing this branch are preserved. YouTube will share this same
            // normal Edge profile from now on.
            string profileDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "InfoDisplayApp",
                "BrowserProfiles",
                "Philo");

            _browser = new BrowserWindow(
                PhiloUrl,
                profileDirectory,
                9222,
                owner,
                viewport);

            owner.Move += OwnerBoundsChanged;
            owner.Resize += OwnerBoundsChanged;
            owner.Activated += OwnerActivated;
            viewport.Resize += OwnerBoundsChanged;
            viewport.LocationChanged += OwnerBoundsChanged;

            _owner = owner;
            _viewport = viewport;

            _zOrderTimer = new System.Windows.Forms.Timer { Interval = 250 };
            _zOrderTimer.Tick += (_, _) => RepositionVisibleWindows();
        }

        private readonly Form _owner;
        private readonly Control _viewport;

        public async Task InitializeAsync()
        {
            ThrowIfDisposed();

            await _browser.StartAsync();
            await _browser.NavigateAsync(PhiloUrl);
            await _browser.SetMutedAsync(false);
            _browser.Show();
            _zOrderTimer.Start();
        }

        public async Task ShowPhiloAsync()
        {
            ThrowIfDisposed();

            _browser.Show();
            await _browser.NavigateAsync(PhiloUrl);
            await _browser.SetMutedAsync(false);
        }

        public async Task ShowYouTubeAsync()
        {
            ThrowIfDisposed();

            _browser.Show();
            await _browser.NavigateAsync(YouTubeUrl);
            await _browser.SetMutedAsync(false);
        }

        public async Task HideAllAsync()
        {
            if (_disposed)
                return;

            await _browser.SetMutedAsync(true);
            _browser.Hide();
        }

        public void RepositionVisibleWindows()
        {
            if (_disposed)
                return;

            _browser.PositionIfVisible();
        }

        private void OwnerBoundsChanged(object? sender, EventArgs e) =>
            RepositionVisibleWindows();

        private void OwnerActivated(object? sender, EventArgs e) =>
            RepositionVisibleWindows();

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
            _zOrderTimer.Stop();
            _zOrderTimer.Dispose();

            _owner.Move -= OwnerBoundsChanged;
            _owner.Resize -= OwnerBoundsChanged;
            _owner.Activated -= OwnerActivated;
            _viewport.Resize -= OwnerBoundsChanged;
            _viewport.LocationChanged -= OwnerBoundsChanged;

            _browser.Dispose();
        }

        private sealed class BrowserWindow : IDisposable
        {
            private readonly string _initialUrl;
            private readonly string _profileDirectory;
            private readonly int _debugPort;
            private readonly Form _owner;
            private readonly Control _viewport;
            private readonly HashSet<int> _ownedProcessIds = new();

            private Process? _launcherProcess;
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
            private const long WsOverlappedWindow = 0x00CF0000L;
            private const long WsPopup = unchecked((long)0x80000000L);
            private const long WsVisible = 0x10000000L;
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
            private static readonly IntPtr HwndTopMost = new(-1);

            public BrowserWindow(
                string initialUrl,
                string profileDirectory,
                int debugPort,
                Form owner,
                Control viewport)
            {
                _initialUrl = initialUrl;
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
                HashSet<int> processesBeforeLaunch = SnapshotEdgeProcessIds();

                string arguments =
                    $"--app=\"{_initialUrl}\" " +
                    "--start-fullscreen " +
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
                    ?? throw new InvalidOperationException("Unable to start external Edge viewer.");

                _windowHandle = await WaitForNewEdgeWindowAsync(
                    windowsBeforeLaunch,
                    TimeSpan.FromSeconds(20));

                if (_windowHandle == IntPtr.Zero)
                {
                    throw new InvalidOperationException(
                        "Edge was launched, but InfoDisplay could not locate its viewer window.");
                }

                foreach (int processId in SnapshotEdgeProcessIds())
                {
                    if (!processesBeforeLaunch.Contains(processId))
                        _ownedProcessIds.Add(processId);
                }

                GetWindowThreadProcessId(_windowHandle, out uint windowPid);
                if (windowPid != 0)
                    _ownedProcessIds.Add((int)windowPid);

                ConfigureWindow();
                Debug.WriteLine($"External Edge viewer started on DevTools port {_debugPort}.");
            }

            public async Task NavigateAsync(string url)
            {
                if (_disposed || _windowHandle == IntPtr.Zero || !IsWindow(_windowHandle))
                    return;

                try
                {
                    string? currentUrl = await GetCurrentUrlAsync();
                    if (!string.IsNullOrWhiteSpace(currentUrl) &&
                        UrlMatchesService(currentUrl, url))
                    {
                        return;
                    }

                    JsonElement? target = await GetPageTargetAsync();
                    if (target == null ||
                        !target.Value.TryGetProperty("webSocketDebuggerUrl", out JsonElement wsProperty))
                    {
                        throw new InvalidOperationException("No controllable Edge page target was found.");
                    }

                    string? websocketUrl = wsProperty.GetString();
                    if (string.IsNullOrWhiteSpace(websocketUrl))
                        throw new InvalidOperationException("Edge did not expose a DevTools websocket URL.");

                    await SendDevToolsCommandAsync(
                        websocketUrl,
                        "Page.navigate",
                        new { url });

                    Debug.WriteLine($"External Edge viewer navigated to {url}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"External Edge viewer navigation failed: {ex.Message}");
                    throw;
                }
            }

            private static bool UrlMatchesService(string currentUrl, string requestedUrl)
            {
                if (!Uri.TryCreate(currentUrl, UriKind.Absolute, out Uri? current) ||
                    !Uri.TryCreate(requestedUrl, UriKind.Absolute, out Uri? requested))
                {
                    return currentUrl.StartsWith(requestedUrl, StringComparison.OrdinalIgnoreCase);
                }

                return current.Host.Equals(requested.Host, StringComparison.OrdinalIgnoreCase) ||
                    current.Host.EndsWith("." + requested.Host, StringComparison.OrdinalIgnoreCase);
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
                            Debug.WriteLine($"External Edge viewer mute update failed: {ex.Message}");
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
                style &= ~WsOverlappedWindow;
                style |= WsPopup | WsVisible;
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
                    SwpNoMove |
                    SwpNoSize |
                    SwpNoActivate |
                    SwpFrameChanged |
                    SwpShowWindow);

                Position();
            }

            private async Task<string?> GetCurrentUrlAsync()
            {
                JsonElement? target = await GetPageTargetAsync();
                if (target == null ||
                    !target.Value.TryGetProperty("url", out JsonElement urlProperty))
                {
                    return null;
                }

                return urlProperty.GetString();
            }

            private async Task<bool> EvaluateJavaScriptAsync(string expression)
            {
                JsonElement? target = await GetPageTargetAsync();
                if (target == null ||
                    !target.Value.TryGetProperty("webSocketDebuggerUrl", out JsonElement wsProperty))
                {
                    return false;
                }

                string? websocketUrl = wsProperty.GetString();
                if (string.IsNullOrWhiteSpace(websocketUrl))
                    return false;

                await SendDevToolsCommandAsync(
                    websocketUrl,
                    "Runtime.evaluate",
                    new { expression, returnByValue = true });

                return true;
            }

            private async Task<JsonElement?> GetPageTargetAsync()
            {
                string endpoint = $"http://127.0.0.1:{_debugPort}/json";
                string targetsJson = await DevToolsHttp.GetStringAsync(endpoint);

                using JsonDocument targets = JsonDocument.Parse(targetsJson);

                return targets.RootElement
                    .EnumerateArray()
                    .Where(item =>
                        item.TryGetProperty("type", out JsonElement type) &&
                        type.GetString() == "page")
                    .Select(item => item.Clone())
                    .Cast<JsonElement?>()
                    .FirstOrDefault();
            }

            private static async Task SendDevToolsCommandAsync(
                string websocketUrl,
                string method,
                object parameters)
            {
                using ClientWebSocket socket = new();
                using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(3));

                await socket.ConnectAsync(new Uri(websocketUrl), timeout.Token);

                string command = JsonSerializer.Serialize(new
                {
                    id = 1,
                    method,
                    @params = parameters
                });

                byte[] payload = Encoding.UTF8.GetBytes(command);
                await socket.SendAsync(
                    payload,
                    WebSocketMessageType.Text,
                    true,
                    timeout.Token);

                byte[] responseBuffer = new byte[4096];
                await socket.ReceiveAsync(responseBuffer, timeout.Token);
            }

            private static async Task<IntPtr> WaitForNewEdgeWindowAsync(
                HashSet<IntPtr> windowsBeforeLaunch,
                TimeSpan timeout)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                while (stopwatch.Elapsed < timeout)
                {
                    List<EdgeWindowInfo> candidates = GetVisibleEdgeWindows()
                        .Where(window => !windowsBeforeLaunch.Contains(window.Handle))
                        .ToList();

                    if (candidates.Count > 0)
                        return candidates[0].Handle;

                    await Task.Delay(150);
                }

                return IntPtr.Zero;
            }

            private static HashSet<IntPtr> SnapshotVisibleEdgeWindows() =>
                GetVisibleEdgeWindows().Select(window => window.Handle).ToHashSet();

            private static HashSet<int> SnapshotEdgeProcessIds() =>
                Process.GetProcessesByName("msedge")
                    .Select(process =>
                    {
                        int id = process.Id;
                        process.Dispose();
                        return id;
                    })
                    .ToHashSet();

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

                return candidates.FirstOrDefault(File.Exists) ?? "msedge.exe";
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

                    Thread.Sleep(250);

                    foreach (int processId in _ownedProcessIds.ToArray())
                    {
                        try
                        {
                            using Process process = Process.GetProcessById(processId);
                            if (!process.HasExited)
                                process.Kill(entireProcessTree: true);
                        }
                        catch (ArgumentException)
                        {
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"External Edge viewer shutdown issue: {ex.Message}");
                }
                finally
                {
                    _ownedProcessIds.Clear();
                    _launcherProcess?.Dispose();
                    _launcherProcess = null;
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
