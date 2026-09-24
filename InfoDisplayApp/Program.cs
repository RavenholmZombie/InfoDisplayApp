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

            using RemoteDiagnosticsServer diagnostics = new();
            diagnostics.Start();

            Application.Run(new StartupApplicationContext());
        }

        private sealed class StartupApplicationContext : ApplicationContext
        {
            private static readonly TimeSpan MinimumSplashDuration =
                TimeSpan.FromSeconds(3.5);

            private static readonly TimeSpan CrossFadeDuration =
                TimeSpan.FromMilliseconds(650);

            private const int CrossFadeSteps = 26;

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
                    await Task.Delay(250);

                    // Do not reveal the dashboard until the ticker has completed its
                    // initial status/weather work and has a real rendered message
                    // ready for the scrolling animation.
                    _splash.SetStartupStatus("Preparing text ticker...", 85);
                    await _mainForm.WaitForStartupReadyAsync();

                    _splash.SetStartupStatus("Finishing startup...", 95);

                    // Keep the deliberate boot-screen pacing even if all of the
                    // real initialization work finishes unusually quickly.
                    TimeSpan remaining = MinimumSplashDuration - _startupClock.Elapsed;
                    if (remaining > TimeSpan.Zero)
                        await Task.Delay(remaining);

                    _splash.SetStartupStatus("Ready", 100);
                    await Task.Delay(150);

                    // A borderless Maximized WinForms window still uses the screen's
                    // working area, which leaves the Windows taskbar uncovered.
                    // Switch to Normal and explicitly occupy the monitor's full bounds
                    // so InfoDisplay behaves like a true kiosk/fullscreen application.
                    Screen targetScreen = Screen.FromHandle(_mainForm.Handle);
                    _mainForm.WindowState = FormWindowState.Normal;
                    _mainForm.Bounds = targetScreen.Bounds;
                    _mainForm.ShowInTaskbar = false;
                    _mainForm.Enabled = true;

                    await CrossFadeToMainAsync();

                    _mainForm.TopMost = _mainWasTopMost;
                    _mainForm.BringToFront();
                    _mainForm.Activate();

                    _startupCompleted = true;
                    _splash.Close();

                    // The sound now happens only after the fully-loaded main form
                    // has actually finished fading in and is visible to the user.
                    _mainForm.NotifyStartupVisible();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"InfoDisplay reveal failed: {ex}");
                    ExitThread();
                }
            }

            private async Task CrossFadeToMainAsync()
            {
                if (_mainForm == null)
                    return;

                double splashStartOpacity = Math.Clamp(_splash.Opacity, 0.0, 1.0);
                int stepDelay = Math.Max(
                    1,
                    (int)(CrossFadeDuration.TotalMilliseconds / CrossFadeSteps));

                for (int step = 0; step <= CrossFadeSteps; step++)
                {
                    double progress = (double)step / CrossFadeSteps;
                    double eased = progress * progress * (3.0 - (2.0 * progress));

                    _mainForm.Opacity = eased;
                    _splash.Opacity = splashStartOpacity * (1.0 - eased);

                    await Task.Delay(stepDelay);
                }

                _mainForm.Opacity = 1.0;
                _splash.Opacity = 0.0;
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
