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
                Debug.WriteLine("Tapo camera: VLC encountered a playback error.");
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
                Debug.WriteLine(
                    $"Camera configuration file not found: {configPath}");

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
                    Debug.WriteLine(
                        "camera.conf is missing required settings.");

                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Failed to read camera.conf: {ex}");

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
                Debug.WriteLine(
                    "Camera cannot start because camera.conf is missing or invalid.");

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

                // Let the VideoView become visible and finish its layout before asking
                // LibVLC to attach/start the native video output.
                await Task.Yield();

                cancellationToken.ThrowIfCancellationRequested();

                _media?.Dispose();
                _media = new Media(_libVLC, new Uri(RtspUrl));

                MediaPlayer player = _mediaPlayer;
                Media media = _media;

                // LibVLC normally returns from Play immediately, but keeping the native
                // start call off the WinForms UI thread prevents a stalled RTSP/native
                // VLC call from freezing the entire dashboard.
                bool started = await Task.Run(
                    () => player.Play(media),
                    cancellationToken);

                Debug.WriteLine(
                    $"Tapo camera: VLC Play returned {(started ? "success" : "failure")}.");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Tapo camera: start cancelled.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Tapo camera: failed to start stream: {ex}");
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
                Debug.WriteLine($"Tapo camera: failed to stop stream: {ex}");
            }
        }

        public void RestartCamera()
        {
            StopCamera();
            StartCamera();
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
