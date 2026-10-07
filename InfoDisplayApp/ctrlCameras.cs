using LibVLCSharp.Shared;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InfoDisplayApp
{
    public partial class ctrlCameras : UserControl
    {
        private LibVLC? _libVLC;
        private MediaPlayer? _mediaPlayer;
        private Media? _media;
        private CancellationTokenSource? _cameraStartCancellation;
        private bool _cameraConfigured;
        private bool _cameraStarting;

        private string _cameraIp = "";
        private string _cameraUsername = "";
        private string _cameraPassword = "";
        private string _cameraStream = "stream1";

        public bool IsConfigured => _cameraConfigured;

        private string RtspUrl =>
            $"rtsp://{Uri.EscapeDataString(_cameraUsername)}:" +
            $"{Uri.EscapeDataString(_cameraPassword)}@" +
            $"{_cameraIp}/{_cameraStream}";

        public ctrlCameras()
        {
            InitializeComponent();

            _cameraConfigured = LoadCameraConfig();

            Core.Initialize();

            _libVLC = new LibVLC(
                "--rtsp-tcp",
                "--network-caching=300",
                "--live-caching=300",
                "--no-video-title-show");

            _mediaPlayer = new MediaPlayer(_libVLC);

            _mediaPlayer.Opening += (_, _) =>
                Debug.WriteLine("Tapo camera: VLC opening RTSP stream.");
            _mediaPlayer.Playing += (_, _) =>
                Debug.WriteLine("Tapo camera: VLC playback started.");
            _mediaPlayer.EncounteredError += (_, _) =>
            {
                const string message = "Tapo camera: VLC encountered a playback error.";
                Debug.WriteLine(message);
                AppMessages.Error(message);
            };
            _mediaPlayer.Stopped += (_, _) =>
                Debug.WriteLine("Tapo camera: VLC playback stopped.");

            vlcPlayer.MediaPlayer = _mediaPlayer;
            vlcPlayer.Dock = DockStyle.Fill;

            Disposed += CtrlCameras_Disposed;
        }

        private bool LoadCameraConfig()
        {
            string configPath =
                Path.Combine(AppContext.BaseDirectory, "camera.conf");

            if (!File.Exists(configPath))
            {
                string message = $"Camera configuration file not found: {configPath}";
                Debug.WriteLine(message);
                AppMessages.Warning(message);
                return false;
            }

            try
            {
                foreach (string rawLine in File.ReadAllLines(configPath))
                {
                    string line = rawLine.Trim();

                    if (string.IsNullOrWhiteSpace(line) ||
                        line.StartsWith("#"))
                    {
                        continue;
                    }

                    int separator = line.IndexOf('=');

                    if (separator <= 0)
                        continue;

                    string key =
                        line[..separator].Trim().ToLowerInvariant();

                    string value =
                        line[(separator + 1)..].Trim();

                    switch (key)
                    {
                        case "ip":
                            _cameraIp = value;
                            break;

                        case "username":
                            _cameraUsername = value;
                            break;

                        case "password":
                            _cameraPassword = value;
                            break;

                        case "stream":
                            _cameraStream = value;
                            break;
                    }
                }

                if (string.IsNullOrWhiteSpace(_cameraIp) ||
                    string.IsNullOrWhiteSpace(_cameraUsername) ||
                    string.IsNullOrWhiteSpace(_cameraPassword))
                {
                    const string message = "camera.conf is missing required settings.";
                    Debug.WriteLine(message);
                    AppMessages.Warning(message);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                const string message = "Failed to read camera.conf.";
                Debug.WriteLine($"{message} {ex}");
                AppMessages.Error(message, ex);
                return false;
            }
        }

        private void ctrlCameras_Load(object sender, EventArgs e)
        {
        }

        public void SetMuted(bool muted)
        {
            if (_mediaPlayer == null)
                return;

            _mediaPlayer.Mute = muted;
        }

        public void StartCamera()
        {
            if (!_cameraConfigured)
            {
                const string message = "Camera cannot start because camera.conf is missing or invalid.";
                Debug.WriteLine(message);
                AppMessages.Warning(message);
                return;
            }

            if (_libVLC == null || _mediaPlayer == null)
                return;

            if (_cameraStarting || _mediaPlayer.IsPlaying)
                return;

            _cameraStartCancellation?.Cancel();
            _cameraStartCancellation?.Dispose();
            _cameraStartCancellation = new CancellationTokenSource();

            _ = StartCameraAsync(_cameraStartCancellation.Token);
        }

        private async Task StartCameraAsync(CancellationToken cancellationToken)
        {
            if (_libVLC == null || _mediaPlayer == null)
                return;

            _cameraStarting = true;

            try
            {
                Debug.WriteLine($"Tapo camera: starting RTSP stream at {_cameraIp}/{_cameraStream}.");

                await Task.Yield();

                cancellationToken.ThrowIfCancellationRequested();

                _media?.Dispose();
                _media = new Media(_libVLC, new Uri(RtspUrl));

                MediaPlayer player = _mediaPlayer;
                Media media = _media;

                bool started = await Task.Run(
                    () => player.Play(media),
                    cancellationToken);

                Debug.WriteLine(
                    $"Tapo camera: VLC Play returned {(started ? "success" : "failure")}.");

                if (!started)
                    AppMessages.Error("Tapo camera: VLC could not start the RTSP stream.");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Tapo camera: start cancelled.");
            }
            catch (Exception ex)
            {
                const string message = "Tapo camera: failed to start stream.";
                Debug.WriteLine($"{message} {ex}");
                AppMessages.Error(message, ex);
            }
            finally
            {
                _cameraStarting = false;
            }
        }

        public void StopCamera()
        {
            _cameraStartCancellation?.Cancel();

            if (_mediaPlayer == null)
                return;

            try
            {
                if (_mediaPlayer.IsPlaying)
                    _mediaPlayer.Stop();
            }
            catch (Exception ex)
            {
                const string message = "Tapo camera: failed to stop stream.";
                Debug.WriteLine($"{message} {ex}");
                AppMessages.Warning($"{message} {ex.Message}");
            }
        }

        public void RestartCamera()
        {
            StopCamera();
            StartCamera();
        }

        /// <summary>
        /// Detaches LibVLC from the WinForms control immediately and releases the
        /// native RTSP player on a worker thread. LibVLC Stop() can block while an
        /// active RTSP session is being torn down, so shutdown must never wait for
        /// it on the UI thread.
        /// </summary>
        public void BeginShutdownCleanup()
        {
            _cameraStartCancellation?.Cancel();
            _cameraStartCancellation?.Dispose();
            _cameraStartCancellation = null;
            _cameraStarting = false;

            MediaPlayer? player = _mediaPlayer;
            Media? media = _media;
            LibVLC? libVlc = _libVLC;

            _mediaPlayer = null;
            _media = null;
            _libVLC = null;

            // Disconnect the native player HWND before frmMain starts disposing
            // controls. This also prevents CtrlCameras_Disposed from stopping the
            // same player a second time on the UI thread.
            try { vlcPlayer.MediaPlayer = null; }
            catch (Exception ex)
            {
                Debug.WriteLine($"Tapo camera: unable to detach VLC view during shutdown: {ex.Message}");
            }

            _ = Task.Run(() =>
            {
                try
                {
                    if (player != null)
                    {
                        try { player.Mute = true; } catch { }
                        try { player.Stop(); } catch { }
                        try { player.Dispose(); } catch { }
                    }

                    try { media?.Dispose(); } catch { }
                    try { libVlc?.Dispose(); } catch { }

                    Debug.WriteLine("Tapo camera: background shutdown cleanup completed.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Tapo camera: background shutdown cleanup failed: {ex}");
                }
            });
        }

        private void CtrlCameras_Disposed(object? sender, EventArgs e)
        {
            _cameraStartCancellation?.Cancel();
            _cameraStartCancellation?.Dispose();
            _cameraStartCancellation = null;

            if (_mediaPlayer != null)
            {
                try
                {
                    _mediaPlayer.Stop();
                }
                catch
                {
                    // Best effort during shutdown.
                }

                _mediaPlayer.Dispose();
                _mediaPlayer = null;
            }

            _media?.Dispose();
            _media = null;

            if (_libVLC != null)
            {
                _libVLC.Dispose();
                _libVLC = null;
            }
        }

        private void vlcPlayer_Click(object sender, EventArgs e)
        {
        }
    }
}
