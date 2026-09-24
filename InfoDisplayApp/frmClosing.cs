using InfoDisplayApp.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Text;
using System.Windows.Forms;

namespace InfoDisplayApp
{
    public partial class frmClosing : Form
    {
        private int _countdown = 7; // Seconds
        private bool _isRestarting = false; // Tells the form whether to restart or exit the application.
        private SoundPlayer? _exitSoundPlayer;
        private readonly Stopwatch _shutdownClock = Stopwatch.StartNew();
        private string ShutdownLogPath =>
            Path.Combine(AppContext.BaseDirectory, "logs",
                $"InfoScreen-SHUTDOWN-{Environment.ProcessId}.log");

        private void LogShutdown(string message)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ShutdownLogPath)!);
                File.AppendAllText(
                    ShutdownLogPath,
                    $"{DateTime.Now:O} +{_shutdownClock.Elapsed.TotalMilliseconds:F0}ms {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to write shutdown diagnostics: {ex}");
            }
        }
        public frmClosing()
        {
            InitializeComponent();
        }

        private async void frmClosing_Load(object sender, EventArgs e)
        {
            LogShutdown($"frmClosing_Load entered; restarting={_isRestarting}.");
            _exitSoundPlayer?.Dispose();
            _exitSoundPlayer = new SoundPlayer(Resources.sfx_exit);
            _exitSoundPlayer.Load();
            _exitSoundPlayer.Play();
            LogShutdown("Exit sound started.");
            // Do not use the WinForms actionTimer for shutdown timing. WM_TIMER
            // delivery has proven unreliable under InfoScreen's media workload.
            actionTimer.Stop();
            LogShutdown($"Async closing countdown started at {_countdown} seconds.");
            Cursor = Cursors.WaitCursor;

            // Label handling
            if (_isRestarting)
            {
                // Case - Restarting
                statusLabel.Text = "We'll be right back!\nRestarting InfoScreen...";
            }
            else
            {
                // Case - Exiting
                statusLabel.Text = "See you later!\nClosing to Windows...";
            }

            await RunClosingCountdownAsync();
        }

        private async Task RunClosingCountdownAsync()
        {
            while (_countdown > 0)
            {
                await Task.Delay(1000);

                if (IsDisposed || Disposing)
                    return;

                _countdown--;
                LogShutdown($"Async closing countdown tick; remaining={_countdown}.");
            }

            LogShutdown("Async countdown reached zero.");
            DisposeExitSoundPlayer();
            LogShutdown("Exit sound disposed.");

            if (_isRestarting)
            {
                LogShutdown("Calling Application.Restart().");
                Application.Restart();
                LogShutdown("Application.Restart() returned.");
            }
            else
            {
                LogShutdown("Calling Application.Exit().");
                Application.Exit();
                LogShutdown("Application.Exit() returned.");
            }
        }

        private void actionTimer_Tick(object sender, EventArgs e)
        {
            // Intentionally unused. Kept wired in the designer so the diagnostic
            // fix does not require designer churn; shutdown timing is Task-based.
        }

        private void DisposeExitSoundPlayer()
        {
            try { _exitSoundPlayer?.Stop(); }
            catch { }
            _exitSoundPlayer?.Dispose();
            _exitSoundPlayer = null;
        }

        public bool setRestarting(bool isRestarting)
        {
            _isRestarting = isRestarting;
            return _isRestarting;
        }
    }
}
