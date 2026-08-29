using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace InfoDisplayApp
{
    public partial class ctrlCameras : UserControl
    {
        private LibVLC? _libVLC;
        private MediaPlayer? _mediaPlayer;
        private bool _cameraConfigured;

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
                "--live-caching=300");

            _mediaPlayer = new MediaPlayer(_libVLC);

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

            if (_mediaPlayer.IsPlaying)
                return;

            using var media =
                new Media(_libVLC, new Uri(RtspUrl));

            _mediaPlayer.Play(media);
        }

        public void StopCamera()
        {
            if (_mediaPlayer == null)
                return;

            if (_mediaPlayer.IsPlaying)
                _mediaPlayer.Stop();
        }

        public void RestartCamera()
        {
            StopCamera();
            StartCamera();
        }

        private void CtrlCameras_Disposed(object? sender, EventArgs e)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Dispose();
                _mediaPlayer = null;
            }

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
