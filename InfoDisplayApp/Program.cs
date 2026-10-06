using System.Diagnostics;
using InfoDisplayApp.Properties;
using InfoDisplayApp.Services;

namespace InfoDisplayApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            RemoteDiagnosticsServer? diagnosticsServer = null;

            try
            {
                if (AppSettings.Current.Diagnostics.RemoteServer)
                {
                    diagnosticsServer = new RemoteDiagnosticsServer();
                    diagnosticsServer.Start();
                }

                Application.Run(new StartupApplicationContext());
            }
            finally
            {
                diagnosticsServer?.Dispose();
            }
        }

        private sealed class StartupApplicationContext : ApplicationContext
        {
            private static readonly TimeSpan MinimumLoadingDuration =
                TimeSpan.FromSeconds(3.5);

            private static readonly TimeSpan CrossFadeDuration =
                TimeSpan.FromMilliseconds(650);

            private const int CrossFadeSteps = 26;

            private readonly frmIntro _intro;
            private frmNewLoading? _loading;
            private readonly Stopwatch _loadingClock = new();
            private frmMain? _mainForm;
            private bool _mainWasTopMost;
            private bool _startupCompleted;
            private bool _transitioningFromIntro;

            public StartupApplicationContext()
            {
                _intro = new frmIntro();
                _intro.IntroCompleted += Intro_Completed;
                _intro.FormClosed += Intro_FormClosed;
                MainForm = _intro;
                _intro.Show();
            }

            private async void Intro_Completed(object? sender, EventArgs e)
            {
                if (_transitioningFromIntro)
                    return;

                _transitioningFromIntro = true;
                _intro.IntroCompleted -= Intro_Completed;

                try
                {
                    _loading = new frmNewLoading();
                    _loading.FormClosed += Loading_FormClosed;
                    _loading.Show();
                    _loading.BringToFront();
                    _loading.Activate();

                    await Task.Yield();

                    _loadingClock.Restart();
                    _intro.Hide();

                    await StartMainAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"InfoDisplay startup transition failed: {ex}");
                    ExitThread();
                }
            }

            private async Task StartMainAsync()
            {
                if (_loading == null)
                    return;

                try
                {
                    SetLoadingStep("Info Display...");
                    await Task.Delay(350);

                    await Task.Yield();

                    _mainForm = new frmMain();
                    AppMessages.Initialize(_mainForm);

                    _mainWasTopMost = _mainForm.TopMost;
                    _mainForm.TopMost = false;
                    // Keep the dashboard fully rendered behind the loading screen.
                    // The reveal is performed by fading the loading screen away.
                    _mainForm.Opacity = 1.0;
                    _mainForm.ShowInTaskbar = false;
                    _mainForm.Enabled = false;
                    _mainForm.Shown += MainForm_Shown;

                    MainForm = _mainForm;

                    SetLoadingStep("display modules...");
                    await Task.Delay(350);

                    SetLoadingStep("media services...");
                    await Task.Yield();

                    _mainForm.Show();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"InfoDisplay startup failed: {ex}");
                    SetLoadingStep("failed.");

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
                if (_mainForm == null || _loading == null || _startupCompleted)
                    return;

                _mainForm.Shown -= MainForm_Shown;

                try
                {
                    SetLoadingStep("checking Internet connection...");
                    await _mainForm.CheckInitialConnectivityAsync();

                    SetLoadingStep("weather and status services...");
                    await Task.Delay(250);

                    SetLoadingStep("preparing text ticker...");
                    await _mainForm.PrepareTickerForRevealAsync();
                    await _mainForm.WaitForStartupReadyAsync();

                    SetLoadingStep("final startup tasks...");

                    TimeSpan remaining =
                        MinimumLoadingDuration - _loadingClock.Elapsed;

                    if (remaining > TimeSpan.Zero)
                        await Task.Delay(remaining);

                    SetLoadingStep("complete...");
                    await Task.Delay(150);

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

                    _loading.FormClosed -= Loading_FormClosed;
                    _loading.Dispose();
                    _loading = null;

                    _intro.FormClosed -= Intro_FormClosed;
                    _intro.Close();

                    _mainForm.NotifyStartupVisible();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"InfoDisplay reveal failed: {ex}");
                    ExitThread();
                }
            }

            private void SetLoadingStep(string step)
            {
                _loading?.SetStartupStatus(step);
            }

            private async Task CrossFadeToMainAsync()
            {
                if (_mainForm == null || _loading == null)
                    return;

                // Keep frmMain fully rendered underneath frmNewLoading. Fading the
                // top loading window exposes the finished dashboard naturally and
                // avoids WinForms' unreliable transparent-window reveal behavior.
                _mainForm.Opacity = 1.0;
                _mainForm.BringToFront();
                _loading.BringToFront();

                // frmIntro hides the cursor once at startup. Keep that hide in
                // effect while frmMain is prepared behind the loading screen,
                // then balance it exactly once when the loading fade begins.
                Cursor.Show();

                double loadingStartOpacity =
                    Math.Clamp(_loading.Opacity, 0.0, 1.0);

                int stepDelay = Math.Max(
                    1,
                    (int)(CrossFadeDuration.TotalMilliseconds / CrossFadeSteps));

                for (int step = 0; step <= CrossFadeSteps; step++)
                {
                    double progress = (double)step / CrossFadeSteps;
                    double eased =
                        progress * progress * (3.0 - (2.0 * progress));

                    _loading.Opacity =
                        loadingStartOpacity * (1.0 - eased);

                    await Task.Delay(stepDelay);
                }

                _loading.Opacity = 0.0;
                _mainForm.BringToFront();
            }

            private void Intro_FormClosed(object? sender, FormClosedEventArgs e)
            {
                if (!_startupCompleted && _loading == null)
                    ExitThread();
            }

            private void Loading_FormClosed(object? sender, FormClosedEventArgs e)
            {
                if (!_startupCompleted)
                    ExitThread();
            }
        }
    }
}
