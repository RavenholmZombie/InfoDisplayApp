using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace InfoDisplayApp.Services
{
    internal sealed class DisplayDiagnosticsMonitor : IDisposable
    {
        private const int SM_CMONITORS = 80;
        private const int SM_REMOTESESSION = 0x1000;

        private bool _started;

        public void Start()
        {
            if (!AppSettings.Current.Diagnostics.DisplayMonitor)
                return;

            if (_started)
                return;

            _started = true;
            AudioPathology.Log("DISPLAY: diagnostics monitor starting.");
            LogSnapshot("initial");

            SystemEvents.DisplaySettingsChanging += SystemEvents_DisplaySettingsChanging;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;

            AudioPathology.Log("DISPLAY: SystemEvents display/session callbacks subscribed.");
        }

        public void Dispose()
        {
            if (!_started)
                return;

            _started = false;
            SystemEvents.DisplaySettingsChanging -= SystemEvents_DisplaySettingsChanging;
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;

            AudioPathology.Log("DISPLAY: diagnostics monitor disposed.");
        }

        private void SystemEvents_DisplaySettingsChanging(object? sender, EventArgs e)
        {
            AudioPathology.Log("DISPLAY EVENT: DisplaySettingsChanging.");
            LogSnapshot("DisplaySettingsChanging");
        }

        private void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
        {
            AudioPathology.Log("DISPLAY EVENT: DisplaySettingsChanged.");
            LogSnapshot("DisplaySettingsChanged");
        }

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            AudioPathology.Log($"DISPLAY EVENT: SessionSwitch reason={e.Reason}.");
            LogSnapshot($"SessionSwitch/{e.Reason}");
        }

        private static void LogSnapshot(string reason)
        {
            try
            {
                Screen[] screens = Screen.AllScreens;
                AudioPathology.Log(
                    $"DISPLAY SNAPSHOT ({reason}): screenCount={screens.Length}; " +
                    $"systemMonitorCount={GetSystemMetrics(SM_CMONITORS)}; " +
                    $"remoteSession={GetSystemMetrics(SM_REMOTESESSION) != 0}.");

                for (int i = 0; i < screens.Length; i++)
                {
                    Screen screen = screens[i];
                    AudioPathology.Log(
                        $"DISPLAY SCREEN[{i}] ({reason}): device='{screen.DeviceName}'; primary={screen.Primary}; " +
                        $"bounds={screen.Bounds.X},{screen.Bounds.Y},{screen.Bounds.Width}x{screen.Bounds.Height}; " +
                        $"workingArea={screen.WorkingArea.X},{screen.WorkingArea.Y},{screen.WorkingArea.Width}x{screen.WorkingArea.Height}; " +
                        $"bitsPerPixel={screen.BitsPerPixel}.");
                }
            }
            catch (Exception ex)
            {
                AudioPathology.Log($"DISPLAY SNAPSHOT ({reason}) failed: {ex}");
            }
        }

        private static string DescribeDeviceChange(uint eventType) => eventType switch
        {
            1 => "ConfigurationChanged",
            2 => "DeviceArrival",
            3 => "DeviceRemoval",
            4 => "Docking",
            _ => "Unknown"
        };

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);
    }
}
