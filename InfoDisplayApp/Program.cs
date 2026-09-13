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
            private readonly frmSplash _splash;
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

                try
                {
                    _splash.SetStartupStatus("Preparing Info Display...", 25);
                    await Task.Yield();

                    _mainForm = new frmMain();
                    AppMessages.Initialize(_mainForm);

                    _mainWasTopMost = _mainForm.TopMost;
                    _mainForm.TopMost = false;
                    _mainForm.Opacity = 0;
                    _mainForm.ShowInTaskbar = false;
                    _mainForm.Enabled = false;
                    _mainForm.Shown += MainForm_Shown;

                    MainForm = _mainForm;

                    _splash.SetStartupStatus("Loading display modules...", 55);

                    // Showing the form while fully transparent gives WinForms and
                    // the child controls their normal Load/Shown lifecycle without
                    // exposing a half-built dashboard to the TV.
                    _mainForm.Show();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"InfoDisplay startup failed: {ex}");
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
                    _splash.SetStartupStatus("Finishing startup...", 85);

                    // Yield once more so controls created from frmMain_Load can
                    // process their first queued UI work before the dashboard is
                    // revealed.
                    await Task.Yield();
                    await Task.Delay(150);

                    _splash.SetStartupStatus("Ready", 100);

                    _mainForm.Enabled = true;
                    _mainForm.ShowInTaskbar = true;
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
                    System.Diagnostics.Debug.WriteLine($"InfoDisplay reveal failed: {ex}");
                    ExitThread();
                }
            }

            private void Splash_FormClosed(object? sender, FormClosedEventArgs e)
            {
                _splash.FormClosed -= Splash_FormClosed;

                // If the splash disappears before the main form is ready, treat
                // that as a cancelled/failed startup instead of leaving an
                // invisible application process running in the background.
                if (!_startupCompleted && _mainForm is not { IsDisposed: false })
                    ExitThread();
            }
        }
    }
}
