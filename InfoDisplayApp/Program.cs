using System.Diagnostics;

namespace InfoDisplayApp
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.Run(new StartupApplicationContext());
        }

        private sealed class StartupApplicationContext : ApplicationContext
        {
            private static readonly TimeSpan MinimumSplashDuration =
                TimeSpan.FromSeconds(3.5);

            private readonly frmSplash _splash;
            private readonly Stopwatch _startupClock = new();
            private frmMain? _mainForm;
            private bool _mainWasTopMost;
            private bool _startupCompleted;

            public StartupApplicationContext()
            {
                _splash = new frmSplash();
                _splash.Shown += Splash_Shown;
                _splash.FormClosed += Splash_FormClosed;
                _splash.Show();
            }

            private async void Splash_Shown(object? sender, EventArgs e)
            {
                _splash.Shown -= Splash_Shown;
                _startupClock.Restart();

                try
                {
                    _splash.SetStartupStatus("Preparing Info Display...", 15);
                    await Task.Delay(350);

                    _mainForm = new frmMain();
                    AppMessages.Initialize(_mainForm);

                    _mainWasTopMost = _mainForm.TopMost;
                    _mainForm.TopMost = false;
                    _mainForm.Opacity = 0;
                    _mainForm.ShowInTaskbar = false;
                    _mainForm.Enabled = false;
                    _mainForm.Shown += MainForm_Shown;

                    MainForm = _mainForm;

                    _splash.SetStartupStatus("Loading display modules...", 40);
                    await Task.Delay(350);
                    _splash.SetStartupStatus("Starting media services...", 60);

                    // Showing the form while fully transparent gives WinForms and
                    // the child controls their normal Load/Shown lifecycle without
                    // exposing a half-built dashboard to the TV.
                    _mainForm.Show();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"InfoDisplay startup failed: {ex}");
                    _splash.SetStartupStatus("Startup failed.", 100);

                    MessageBox.Show(
                        $"InfoDisplay could not finish starting.\r\n\r\n{ex.Message}",
                        "InfoDisplay Startup",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    ExitThread();
                }
            }

            private async void MainForm_Shown(object? sender, EventArgs e)
            {
                if (_mainForm == null || _startupCompleted)
                    return;

                _mainForm.Shown -= MainForm_Shown;

                try
                {
                    _splash.SetStartupStatus("Loading weather and status services...", 75);
                    await Task.Delay(450);

                    _splash.SetStartupStatus("Finishing startup...", 90);

                    // Ensure the splash remains visible long enough to read as an
                    // intentional boot screen even on a fast development PC.
                    TimeSpan remaining = MinimumSplashDuration - _startupClock.Elapsed;
                    if (remaining > TimeSpan.Zero)
                        await Task.Delay(remaining);

                    _splash.SetStartupStatus("Ready", 100);
                    await Task.Delay(180);

                    // A borderless Maximized WinForms window still uses the screen's
                    // working area, which leaves the Windows taskbar uncovered.
                    // Switch to Normal and explicitly occupy the monitor's full bounds
                    // so InfoDisplay behaves like a true kiosk/fullscreen application.
                    Screen targetScreen = Screen.FromHandle(_mainForm.Handle);
                    _mainForm.WindowState = FormWindowState.Normal;
                    _mainForm.Bounds = targetScreen.Bounds;

                    _mainForm.Enabled = true;
                    _mainForm.ShowInTaskbar = false;
                    _mainForm.Opacity = 1;
                    _mainForm.TopMost = _mainWasTopMost;
                    _mainForm.BringToFront();
                    _mainForm.Activate();

                    _startupCompleted = true;

                    // The sound now happens only after the fully-loaded main form
                    // has actually been revealed to the user.
                    _mainForm.NotifyStartupVisible();

                    _splash.Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"InfoDisplay reveal failed: {ex}");
                    ExitThread();
                }
            }

            private void Splash_FormClosed(object? sender, FormClosedEventArgs e)
            {
                _splash.FormClosed -= Splash_FormClosed;

                // If the splash disappears before the normal reveal path finishes,
                // do not leave an invisible main window/process behind.
                if (!_startupCompleted)
                    ExitThread();
            }
        }
    }
}
