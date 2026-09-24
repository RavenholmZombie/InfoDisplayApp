using InfoDisplayApp.Properties;
using InfoDisplayApp.Services;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace InfoDisplayApp
{
    public partial class frmClosing : Form
    {
        private int _countdown = 7; // Seconds
        private bool _isRestarting = false; // Tells the form whether to restart or exit the application.
        private WaveOutEvent? _exitAudioOutput;
        private WaveFileReader? _exitAudioReader;
        private MemoryStream? _exitAudioStream;
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
            StartExitAudio();
            LogShutdown("Exit sound started with NAudio waveOut.");
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

        private void StartExitAudio()
        {
            DisposeExitSoundPlayer();

            byte[] wavBytes;
            using (Stream resourceStream = Resources.sfx_exit)
            using (MemoryStream copy = new())
            {
                resourceStream.Position = 0;
                resourceStream.CopyTo(copy);
                wavBytes = copy.ToArray();
            }

            AudioPathology.InspectWave("SHUTDOWN", wavBytes);

            _exitAudioStream = new MemoryStream(wavBytes, writable: false);
            _exitAudioReader = new WaveFileReader(_exitAudioStream);
            _exitAudioOutput = new WaveOutEvent();
            _exitAudioOutput.Init(_exitAudioReader);

            Stopwatch playbackClock = Stopwatch.StartNew();
            _exitAudioOutput.PlaybackStopped += (_, e) =>
            {
                if (_exitAudioReader == null || _exitAudioOutput == null)
                    return;

                AudioPathology.LogPlaybackStopped(
                    "SHUTDOWN",
                    _exitAudioReader,
                    _exitAudioOutput,
                    playbackClock,
                    e.Exception);
            };

            _exitAudioOutput.Play();
            AudioPathology.LogPlaybackStarted(
                "SHUTDOWN",
                _exitAudioReader,
                _exitAudioOutput,
                playbackClock);
        }

        private void DisposeExitSoundPlayer()
        {
            try { _exitAudioOutput?.Stop(); }
            catch { }

            _exitAudioOutput?.Dispose();
            _exitAudioReader?.Dispose();
            _exitAudioStream?.Dispose();

            _exitAudioOutput = null;
            _exitAudioReader = null;
            _exitAudioStream = null;
        }

        public bool setRestarting(bool isRestarting)
        {
            _isRestarting = isRestarting;
            return _isRestarting;
        }
    }
}
