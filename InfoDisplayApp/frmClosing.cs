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

        private void frmClosing_Load(object sender, EventArgs e)
        {
            LogShutdown($"frmClosing_Load entered; restarting={_isRestarting}.");
            _exitSoundPlayer?.Dispose();
            _exitSoundPlayer = new SoundPlayer(Resources.sfx_exit);
            _exitSoundPlayer.Load();
            _exitSoundPlayer.Play();
            LogShutdown("Exit sound started.");
            actionTimer.Start();
            LogShutdown($"Closing countdown started at {_countdown} seconds; timer interval={actionTimer.Interval}ms.");
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
        }

        private void actionTimer_Tick(object sender, EventArgs e)
        {
            _countdown--;
            LogShutdown($"Closing countdown tick; remaining={_countdown}.");
            if (_countdown <= 0)
            {
                actionTimer.Stop();
                LogShutdown("Countdown reached zero; timer stopped.");
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
