using System.Diagnostics;
using System.Runtime.InteropServices;

namespace InfoDisplayApp.Experiments
{
    /// <summary>
    /// Experimental external Firefox host. Rather than Firefox's native
    /// --kiosk mode (which insists on monitor-sized fullscreen), this uses a
    /// dedicated profile with userChrome.css to hide browser chrome while
    /// keeping the top-level window normally resizable.
    /// </summary>
    internal sealed class FirefoxKioskController : IDisposable
    {
        private static readonly IntPtr HWND_TOP = IntPtr.Zero;

        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int SW_RESTORE = 9;

        private readonly Func<Rectangle> _getTargetRectangle;
        private readonly System.Windows.Forms.Timer _watchdog;
        private readonly HashSet<int> _preExistingFirefoxPids = new();

        private IntPtr _firefoxWindow;
        private int _firefoxWindowPid;
        private DateTime _launchTimeUtc;
        private bool _disposed;

        public FirefoxKioskController(Func<Rectangle> getTargetRectangle)
        {
            _getTargetRectangle = getTargetRectangle;

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

            // Do not use --kiosk here. Firefox's native kiosk mode deliberately
            // takes over the whole monitor, which defeats InfoDisplay's reserved
            // bottom information bar. The dedicated profile hides Firefox chrome
            // while leaving the window resizable by Win32.
            startInfo.ArgumentList.Add("-no-remote");
            startInfo.ArgumentList.Add("-profile");
            startInfo.ArgumentList.Add(profileDirectory);
            startInfo.ArgumentList.Add("-new-window");
            startInfo.ArgumentList.Add(uri.AbsoluteUri);

            Process.Start(startInfo)?.Dispose();

            _launchTimeUtc = DateTime.UtcNow;
            _firefoxWindow = IntPtr.Zero;
            _firefoxWindowPid = 0;
            _watchdog.Start();

            OnStatusChanged($"Launching Firefox pseudo-kiosk: {uri.Host}");
        }

        public void ReapplyBounds()
        {
            if (_disposed || _firefoxWindow == IntPtr.Zero || !IsWindow(_firefoxWindow))
                return;

            Rectangle target = _getTargetRectangle();
            if (target.Width <= 0 || target.Height <= 0)
                return;

            ShowWindow(_firefoxWindow, SW_RESTORE);

            // Keep Firefox as a normal (non-topmost) top-level window. It only
            // needs to sit over the browser rectangle; because its height stops
            // before the bottom bar, the InfoDisplay controls remain visible.
            SetWindowPos(
                _firefoxWindow,
                HWND_TOP,
                target.Left,
                target.Top,
                target.Width,
                target.Height,
                SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }

        public void Stop()
        {
            _watchdog.Stop();

            int pid = _firefoxWindowPid;
            _firefoxWindow = IntPtr.Zero;
            _firefoxWindowPid = 0;

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

                if (_firefoxWindow == IntPtr.Zero)
                {
                    if (DateTime.UtcNow - _launchTimeUtc > TimeSpan.FromSeconds(15))
                        OnStatusChanged("Waiting for a Firefox window...");
                    return;
                }

                OnStatusChanged(
                    $"Firefox window attached (PID {_firefoxWindowPid}). " +
                    "InfoDisplay now owns its visible rectangle.");
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

            // Enable userChrome.css for this isolated profile. These prefs are
            // intentionally limited to the experiment profile and do not touch
            // the user's normal Firefox installation/profile.
            string userJsPath = Path.Combine(profileDirectory, "user.js");
            File.WriteAllText(
                userJsPath,
                "user_pref(\"toolkit.legacyUserProfileCustomizations.stylesheets\", true);\r\n" +
                "user_pref(\"browser.tabs.warnOnClose\", false);\r\n" +
                "user_pref(\"browser.shell.checkDefaultBrowser\", false);\r\n");

            string chromeDirectory = Path.Combine(profileDirectory, "chrome");
            Directory.CreateDirectory(chromeDirectory);

            // Hide all normal browser furniture while retaining the content area.
            // Unlike --kiosk/F11, this does not force the top-level window to
            // occupy the entire monitor.
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
