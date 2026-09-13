using System.Diagnostics;
using System.Runtime.InteropServices;

namespace InfoDisplayApp.Experiments
{
    /// <summary>
    /// Experimental Firefox host. Firefox is launched with a dedicated
    /// pseudo-kiosk profile, then its top-level window is re-parented into the
    /// WinForms browser panel. This avoids the z-order fight between two
    /// top-level windows and guarantees the browser cannot cover the bottom bar.
    /// </summary>
    internal sealed class FirefoxKioskController : IDisposable
    {
        private static readonly IntPtr HWND_TOP = IntPtr.Zero;

        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const int SW_RESTORE = 9;

        private const int GWL_STYLE = -16;
        private const long WS_CHILD = 0x40000000L;
        private const long WS_POPUP = 0x80000000L;
        private const long WS_CAPTION = 0x00C00000L;
        private const long WS_THICKFRAME = 0x00040000L;
        private const long WS_SYSMENU = 0x00080000L;
        private const long WS_MINIMIZEBOX = 0x00020000L;
        private const long WS_MAXIMIZEBOX = 0x00010000L;

        private readonly Func<IntPtr> _getHostHandle;
        private readonly Func<Size> _getViewportSize;
        private readonly System.Windows.Forms.Timer _watchdog;
        private readonly HashSet<int> _preExistingFirefoxPids = new();

        private IntPtr _firefoxWindow;
        private int _firefoxWindowPid;
        private DateTime _launchTimeUtc;
        private bool _isEmbedded;
        private bool _disposed;

        public FirefoxKioskController(
            Func<IntPtr> getHostHandle,
            Func<Size> getViewportSize)
        {
            _getHostHandle = getHostHandle;
            _getViewportSize = getViewportSize;

            _watchdog = new System.Windows.Forms.Timer
            {
                Interval = 250
            };
            _watchdog.Tick += Watchdog_Tick;
        }

        public event EventHandler<string>? StatusChanged;

        public bool IsRunning =>
            _firefoxWindow != IntPtr.Zero && IsWindow(_firefoxWindow);

        public string? FirefoxPath { get; private set; }

        public void Launch(string url)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FirefoxKioskController));

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("A valid HTTP or HTTPS URL is required.", nameof(url));
            }

            Stop();

            FirefoxPath = FindFirefoxExecutable()
                ?? throw new FileNotFoundException(
                    "Firefox could not be found. Install Firefox or adjust FindFirefoxExecutable().");

            _preExistingFirefoxPids.Clear();
            foreach (Process process in Process.GetProcessesByName("firefox"))
            {
                try
                {
                    _preExistingFirefoxPids.Add(process.Id);
                }
                finally
                {
                    process.Dispose();
                }
            }

            string profileDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "InfoDisplayApp",
                "FirefoxKioskProfile");

            PreparePseudoKioskProfile(profileDirectory);

            ProcessStartInfo startInfo = new(FirefoxPath)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(FirefoxPath) ?? AppContext.BaseDirectory
            };

            startInfo.ArgumentList.Add("-no-remote");
            startInfo.ArgumentList.Add("-profile");
            startInfo.ArgumentList.Add(profileDirectory);
            startInfo.ArgumentList.Add("-new-window");
            startInfo.ArgumentList.Add(uri.AbsoluteUri);

            Process.Start(startInfo)?.Dispose();

            _launchTimeUtc = DateTime.UtcNow;
            _firefoxWindow = IntPtr.Zero;
            _firefoxWindowPid = 0;
            _isEmbedded = false;
            _watchdog.Start();

            OnStatusChanged($"Launching Firefox pseudo-kiosk: {uri.Host}");
        }

        public void ReapplyBounds()
        {
            if (_disposed || _firefoxWindow == IntPtr.Zero || !IsWindow(_firefoxWindow))
                return;

            IntPtr hostHandle = _getHostHandle();
            Size viewport = _getViewportSize();

            if (hostHandle == IntPtr.Zero || viewport.Width <= 0 || viewport.Height <= 0)
                return;

            if (!_isEmbedded)
            {
                EmbedFirefoxWindow(hostHandle);
                if (!_isEmbedded)
                    return;
            }

            // Once Firefox is a child of pnlBrowserSurface, coordinates are
            // relative to that panel. This makes the browser viewport exactly
            // match the black area and makes it physically impossible for the
            // browser to cover the separate InfoDisplay bottom bar.
            SetWindowPos(
                _firefoxWindow,
                HWND_TOP,
                0,
                0,
                viewport.Width,
                viewport.Height,
                SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }

        private void EmbedFirefoxWindow(IntPtr hostHandle)
        {
            ShowWindow(_firefoxWindow, SW_RESTORE);

            long style = GetWindowStyle(_firefoxWindow);
            style &= ~(WS_POPUP |
                       WS_CAPTION |
                       WS_THICKFRAME |
                       WS_SYSMENU |
                       WS_MINIMIZEBOX |
                       WS_MAXIMIZEBOX);
            style |= WS_CHILD;

            SetWindowStyle(_firefoxWindow, style);

            IntPtr previousParent = SetParent(_firefoxWindow, hostHandle);
            int error = Marshal.GetLastWin32Error();

            // SetParent returning NULL is not automatically failure when the
            // previous parent was the desktop, so verify the relationship.
            if (GetParent(_firefoxWindow) != hostHandle)
            {
                OnStatusChanged(
                    $"Firefox window found, but embedding failed (Win32 error {error}).");
                return;
            }

            _isEmbedded = true;

            SetWindowPos(
                _firefoxWindow,
                HWND_TOP,
                0,
                0,
                Math.Max(1, _getViewportSize().Width),
                Math.Max(1, _getViewportSize().Height),
                SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_FRAMECHANGED);

            OnStatusChanged(
                $"Firefox embedded in InfoDisplay viewport (PID {_firefoxWindowPid}).");
        }

        public void Stop()
        {
            _watchdog.Stop();

            int pid = _firefoxWindowPid;
            _firefoxWindow = IntPtr.Zero;
            _firefoxWindowPid = 0;
            _isEmbedded = false;

            if (pid != 0)
            {
                try
                {
                    using Process process = Process.GetProcessById(pid);
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(3000);
                }
                catch (ArgumentException)
                {
                    // Firefox already exited.
                }
                catch (InvalidOperationException)
                {
                    // Firefox already exited.
                }
                catch (Exception ex)
                {
                    OnStatusChanged($"Unable to stop Firefox cleanly: {ex.Message}");
                }
            }

            OnStatusChanged("Firefox pseudo-kiosk stopped.");
        }

        private void Watchdog_Tick(object? sender, EventArgs e)
        {
            if (_firefoxWindow == IntPtr.Zero || !IsWindow(_firefoxWindow))
            {
                _firefoxWindow = FindNewFirefoxTopLevelWindow(out _firefoxWindowPid);
                _isEmbedded = false;

                if (_firefoxWindow == IntPtr.Zero)
                {
                    if (DateTime.UtcNow - _launchTimeUtc > TimeSpan.FromSeconds(15))
                        OnStatusChanged("Waiting for a Firefox window...");
                    return;
                }

                OnStatusChanged(
                    $"Firefox window attached (PID {_firefoxWindowPid}); embedding into InfoDisplay...");
            }

            ReapplyBounds();
        }

        private IntPtr FindNewFirefoxTopLevelWindow(out int pid)
        {
            IntPtr foundWindow = IntPtr.Zero;
            int foundPid = 0;

            EnumWindows((window, lParam) =>
            {
                if (!IsWindowVisible(window))
                    return true;

                GetWindowThreadProcessId(window, out uint windowPid);
                if (windowPid == 0 || _preExistingFirefoxPids.Contains((int)windowPid))
                    return true;

                try
                {
                    using Process process = Process.GetProcessById((int)windowPid);
                    if (!process.ProcessName.Equals("firefox", StringComparison.OrdinalIgnoreCase))
                        return true;

                    foundWindow = window;
                    foundPid = (int)windowPid;
                    return false;
                }
                catch
                {
                    return true;
                }
            }, IntPtr.Zero);

            pid = foundPid;
            return foundWindow;
        }

        private static void PreparePseudoKioskProfile(string profileDirectory)
        {
            Directory.CreateDirectory(profileDirectory);

            string userJsPath = Path.Combine(profileDirectory, "user.js");
            File.WriteAllText(
                userJsPath,
                "user_pref(\"toolkit.legacyUserProfileCustomizations.stylesheets\", true);\r\n" +
                "user_pref(\"browser.tabs.warnOnClose\", false);\r\n" +
                "user_pref(\"browser.shell.checkDefaultBrowser\", false);\r\n");

            string chromeDirectory = Path.Combine(profileDirectory, "chrome");
            Directory.CreateDirectory(chromeDirectory);

            string userChromePath = Path.Combine(chromeDirectory, "userChrome.css");
            File.WriteAllText(
                userChromePath,
                "#navigator-toolbox { visibility: collapse !important; }\r\n" +
                "#TabsToolbar { visibility: collapse !important; }\r\n" +
                "#titlebar { visibility: collapse !important; }\r\n" +
                "#sidebar-box, #sidebar-splitter { display: none !important; }\r\n" +
                "#statuspanel { display: none !important; }\r\n");
        }

        private static string? FindFirefoxExecutable()
        {
            string[] candidates =
            {
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Mozilla Firefox",
                    "firefox.exe"),
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Mozilla Firefox",
                    "firefox.exe"),
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Mozilla Firefox",
                    "firefox.exe")
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        private static long GetWindowStyle(IntPtr hWnd)
        {
            return IntPtr.Size == 8
                ? GetWindowLongPtr64(hWnd, GWL_STYLE).ToInt64()
                : GetWindowLong32(hWnd, GWL_STYLE);
        }

        private static void SetWindowStyle(IntPtr hWnd, long style)
        {
            if (IntPtr.Size == 8)
                SetWindowLongPtr64(hWnd, GWL_STYLE, new IntPtr(style));
            else
                SetWindowLong32(hWnd, GWL_STYLE, unchecked((int)style));
        }

        private void OnStatusChanged(string message) =>
            StatusChanged?.Invoke(this, message);

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Stop();
            _watchdog.Tick -= Watchdog_Tick;
            _watchdog.Dispose();
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            uint flags);
    }
}
