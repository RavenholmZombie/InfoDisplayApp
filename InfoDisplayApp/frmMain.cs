using InfoDisplayApp.Properties;
using InfoDisplayApp.Services;
using System.Diagnostics;
using System.Media;

namespace InfoDisplayApp
{
    public partial class frmMain : Form
    {
        private ctrlPhiloWebView? _philoView;
        private ctrlCameras? _cameraView;
        private ctrlYouTubeWebView? _youtubeView;
        private ctrlAppsPanel? _appsPanel;
        private ctrlTicker? _normalTicker;
        private ctrlEmergencyTicker? _emergencyTicker;
        private frmBrowser? _browserForm;

        private readonly Random _random = new Random();
        private readonly System.Windows.Forms.Timer _colorTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer _alertPollTimer = new System.Windows.Forms.Timer();
        private readonly NwsAlertService _nwsAlertService = new();
        private readonly Queue<NwsAlertMessage> _pendingAlerts = new();
        private readonly HashSet<string> _seenAlertIds = new(StringComparer.OrdinalIgnoreCase);

        private bool _startupSoundPlayed;
        private bool _alertPollInProgress;
        private bool _emergencyAlertActive;
        private string? _currentAlertId;

        public string tickerMode = "normal";

        private Color _startColor;
        private Color _targetColor;

        private int _fadeStep = 0;
        private const int FadeSteps = 200;

        public frmMain()
        {
            InitializeComponent();

            _startColor = RandomColor();
            _targetColor = RandomColor();

            BackColor = _startColor;

            DoubleBuffered = true;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true
            );

            UpdateStyles();

            _colorTimer.Interval = 30;
            _colorTimer.Tick += ColorTimer_Tick;
            _colorTimer.Start();

            _alertPollTimer.Interval = 60_000;
            _alertPollTimer.Tick += AlertPollTimer_Tick;

            pboxAppsIcon.MouseEnter += pnlBtnApps_MouseEnter;
            pboxAppsIcon.MouseLeave += pnlBtnApps_MouseLeave;
            pboxAppsIcon.Click += pnlBtnApps_Click;
        }

        internal void NotifyStartupVisible()
        {
            if (_startupSoundPlayed || !Visible || Opacity <= 0)
                return;

            _startupSoundPlayed = true;

            try
            {
                using SoundPlayer player = new(Resources.sfx_startup);
                player.Load();
                player.Play();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to play startup sound: {ex}");
            }

            _alertPollTimer.Start();
            _ = PollAlertsAsync();
        }

        private Color RandomColor()
        {
            return Color.FromArgb(
                _random.Next(50, 220),
                _random.Next(50, 220),
                _random.Next(50, 220)
            );
        }

        private void ColorTimer_Tick(object? sender, EventArgs e)
        {
            _fadeStep++;

            double progress = (double)_fadeStep / FadeSteps;

            int r = (int)(_startColor.R +
                (_targetColor.R - _startColor.R) * progress);

            int g = (int)(_startColor.G +
                (_targetColor.G - _startColor.G) * progress);

            int b = (int)(_startColor.B +
                (_targetColor.B - _startColor.B) * progress);

            BackColor = Color.FromArgb(r, g, b);

            if (_fadeStep >= FadeSteps)
            {
                _startColor = _targetColor;
                _targetColor = RandomColor();
                _fadeStep = 0;
            }
        }

        public void ToggleTickerMode(string mode)
        {
            if (mode.Equals("normal", StringComparison.OrdinalIgnoreCase))
            {
                EndEmergencyAlertSequence();
                return;
            }

            _ = PollAlertsAsync(replayActiveAlert: true);
        }

        public void TriggerNationalPeriodicTest()
        {
            if (IsDisposed || _emergencyAlertActive)
                return;

            DateTimeOffset now = DateTimeOffset.Now;

            const string displayText =
                "This is a test of the National Emergency Alert System. " +
                "This system was developed by broadcast and cable operators in voluntary cooperation with the Federal Emergency Management Agency, the Federal Communications Commission, and local authorities to keep you informed in the event of an emergency. " +
                "If this had been an actual emergency, an official message would have followed the tone-alert you heard at the start of this message. No action is required.";

            const string speechText =
                "This is a test of the National Emergency Alert System. " +
                "This system was developed by broadcast and cable operators in voluntary cooperation with the Federal Emergency Management Agency, the Federal Communications Commission, and local authorities to keep you informed in the event of an emergency. " +
                "If this had been an actual emergency, an official message would have followed the tone-alert you heard at the start of this message. No action is required.";

            NwsAlertMessage testAlert = new(
                $"InfoDisplay-NPT-{now:yyyyMMddHHmmssfff}",
                "National Periodic Test",
                displayText,
                speechText,
                "Minor",
                "Expected",
                "InfoDisplay Test Generator",
                "Princeton-Calais, ME",
                now,
                now.AddMinutes(15),
                1);

            _pendingAlerts.Enqueue(testAlert);
            BeginNextEmergencyAlert();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            _philoView = new ctrlPhiloWebView
            {
                Dock = DockStyle.Fill,
                Visible = true
            };

            pnlTV.Controls.Add(_philoView);

            _cameraView = new ctrlCameras
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            pnlTV.Controls.Add(_cameraView);

            _youtubeView = new ctrlYouTubeWebView
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            pnlTV.Controls.Add(_youtubeView);
            _philoView.BringToFront();

            ctrlTimeDate ctrlTimeDate = new ctrlTimeDate
            {
                Dock = DockStyle.Fill
            };
            pnlDateTime.Controls.Add(ctrlTimeDate);

            _normalTicker = new ctrlTicker
            {
                Dock = DockStyle.Fill
            };
            pnlTicker.Controls.Add(_normalTicker);

            ctrlWeather ctrlWeather = new ctrlWeather
            {
                Dock = DockStyle.Fill
            };
            pnlWeather.Controls.Add(ctrlWeather);

            _appsPanel = new ctrlAppsPanel
            {
                Dock = DockStyle.Fill
            };

            pnlApps.Controls.Add(_appsPanel);
            pnlApps.Visible = false;

            UpdateModeButtons(true);
        }

        private async void AlertPollTimer_Tick(object? sender, EventArgs e)
        {
            await PollAlertsAsync();
        }

        private async Task PollAlertsAsync(bool replayActiveAlert = false)
        {
            if (_alertPollInProgress || IsDisposed)
                return;

            _alertPollInProgress = true;

            try
            {
                IReadOnlyList<NwsAlertMessage> alerts =
                    await _nwsAlertService.GetActiveAlertsAsync();

                if (replayActiveAlert && alerts.Count > 0)
                {
                    NwsAlertMessage alert = alerts[0];
                    bool alreadyCurrent =
                        _currentAlertId?.Equals(alert.Id, StringComparison.OrdinalIgnoreCase) == true;
                    bool alreadyQueued = _pendingAlerts.Any(item =>
                        item.Id.Equals(alert.Id, StringComparison.OrdinalIgnoreCase));

                    if (!alreadyCurrent && !alreadyQueued)
                        _pendingAlerts.Enqueue(alert);
                }
                else
                {
                    foreach (NwsAlertMessage alert in alerts)
                    {
                        if (_seenAlertIds.Add(alert.Id))
                            _pendingAlerts.Enqueue(alert);
                    }
                }

                if (!_emergencyAlertActive && _pendingAlerts.Count > 0)
                    BeginNextEmergencyAlert();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to update NWS emergency alerts: {ex}");
            }
            finally
            {
                _alertPollInProgress = false;
            }
        }

        private void BeginNextEmergencyAlert()
        {
            if (_emergencyAlertActive || _pendingAlerts.Count == 0 || IsDisposed)
                return;

            NwsAlertMessage alert = _pendingAlerts.Dequeue();
            _currentAlertId = alert.Id;
            _emergencyAlertActive = true;
            tickerMode = "EAS";

            SetApplicationAudioMuted(true);

            if (_normalTicker != null)
                _normalTicker.Visible = false;

            if (_emergencyTicker != null)
            {
                _emergencyTicker.AlertFinished -= EmergencyTicker_AlertFinished;
                pnlTicker.Controls.Remove(_emergencyTicker);
                _emergencyTicker.Dispose();
            }

            _emergencyTicker = new ctrlEmergencyTicker
            {
                Dock = DockStyle.Fill
            };
            _emergencyTicker.AlertFinished += EmergencyTicker_AlertFinished;

            pnlTicker.Controls.Add(_emergencyTicker);
            _emergencyTicker.BringToFront();
            _emergencyTicker.StartAlert(alert);

            Debug.WriteLine(
                $"EAS ticker started: {alert.EventName} ({alert.Severity}/{alert.Urgency}) " +
                $"matched near {alert.MatchedPoint}.");
        }

        private void EmergencyTicker_AlertFinished(object? sender, EventArgs e)
        {
            if (_emergencyTicker != null)
            {
                _emergencyTicker.AlertFinished -= EmergencyTicker_AlertFinished;
                pnlTicker.Controls.Remove(_emergencyTicker);
                _emergencyTicker.Dispose();
                _emergencyTicker = null;
            }

            _emergencyAlertActive = false;
            _currentAlertId = null;

            if (_pendingAlerts.Count > 0)
            {
                BeginNextEmergencyAlert();
                return;
            }

            RestoreNormalTickerAndAudio();
        }

        private void EndEmergencyAlertSequence()
        {
            _pendingAlerts.Clear();

            if (_emergencyTicker != null)
            {
                _emergencyTicker.AlertFinished -= EmergencyTicker_AlertFinished;
                pnlTicker.Controls.Remove(_emergencyTicker);
                _emergencyTicker.Dispose();
                _emergencyTicker = null;
            }

            _emergencyAlertActive = false;
            _currentAlertId = null;
            RestoreNormalTickerAndAudio();
        }

        private void RestoreNormalTickerAndAudio()
        {
            tickerMode = "normal";

            if (_normalTicker != null)
            {
                _normalTicker.Visible = true;
                _normalTicker.BringToFront();
            }

            SetApplicationAudioMuted(false);
        }

        private void SetApplicationAudioMuted(bool muted)
        {
            if (muted)
            {
                _philoView?.SetMuted(true);
                _youtubeView?.SetMuted(true);
                _cameraView?.SetMuted(true);
                _browserForm?.SetMuted(true);
                return;
            }

            if (_philoView != null)
                _philoView.SetMuted(!_philoView.Visible);

            if (_youtubeView != null)
                _youtubeView.SetMuted(!_youtubeView.Visible);

            if (_cameraView != null)
                _cameraView.SetMuted(!_cameraView.Visible);

            _browserForm?.SetMuted(false);
        }

        public void ShowPhiloMode()
        {
            if (_philoView == null || _cameraView == null)
                return;

            _cameraView.SetMuted(true);
            _cameraView.StopCamera();
            _cameraView.Visible = false;
            _youtubeView?.SetMuted(true);
            if (_youtubeView != null)
                _youtubeView.Visible = false;

            _philoView.SetMuted(_emergencyAlertActive);
            _philoView.Visible = true;
            _philoView.BringToFront();
            pnlApps.Hide();

            UpdateModeButtons(true);
        }

        public void ShowBrowserMode()
        {
            if (_youtubeView == null || _cameraView == null || _philoView == null)
                return;

            _cameraView.SetMuted(true);
            _cameraView.StopCamera();
            _cameraView.Visible = false;
            _philoView.SetMuted(true);
            _youtubeView.Visible = true;
            _youtubeView.SetMuted(true);
            _youtubeView.BringToFront();
            pnlApps.Hide();

            _browserForm = new frmBrowser();
            _browserForm.SetMuted(_emergencyAlertActive);

            try
            {
                _browserForm.ShowDialog(this);
                _browserForm.BringToFront();
            }
            finally
            {
                _browserForm.Dispose();
                _browserForm = null;
            }

            UpdateModeButtons(true);
        }

        public void ShowCameraMode()
        {
            if (_philoView == null || _cameraView == null)
                return;

            _philoView.Visible = false;
            _philoView.SetMuted(true);
            _youtubeView?.SetMuted(true);
            if (_youtubeView != null)
                _youtubeView.Visible = false;

            _cameraView.Visible = true;
            _cameraView.BringToFront();

            _cameraView.SetMuted(_emergencyAlertActive);
            _cameraView.StartCamera();
            pnlApps.Hide();

            UpdateModeButtons(false);
        }

        public void ShowYouTubeMode()
        {
            if (_youtubeView == null || _cameraView == null || _philoView == null)
                return;

            _cameraView.SetMuted(true);
            _cameraView.StopCamera();
            _cameraView.Visible = false;
            _philoView.SetMuted(true);

            _youtubeView.Visible = true;
            _youtubeView.SetMuted(_emergencyAlertActive);
            _youtubeView.BringToFront();
            pnlApps.Hide();

            UpdateModeButtons(true);
        }

        /// <summary>
        /// Stops and disposes all media hosted by the TV panel before the closing
        /// screen appears. This is used for both exit and restart so no stream
        /// audio continues underneath frmClosing.
        /// </summary>
        public void PrepareForShutdown()
        {
            _alertPollTimer.Stop();

            // Mute first so shutdown is silent even if a player takes a moment
            // to release its underlying media session.
            SetApplicationAudioMuted(true);

            try
            {
                _cameraView?.StopCamera();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to stop camera during shutdown: {ex}");
            }

            if (_browserForm != null && !_browserForm.IsDisposed)
            {
                try { _browserForm.SetMuted(true); }
                catch (Exception ex) { Debug.WriteLine($"Unable to mute browser during shutdown: {ex}"); }
            }

            // Dispose the controls themselves rather than only hiding pnlTV.
            // WebView2 and LibVLC can otherwise keep audio/media sessions alive.
            foreach (Control control in pnlTV.Controls.Cast<Control>().ToArray())
            {
                pnlTV.Controls.Remove(control);
                control.Dispose();
            }

            _philoView = null;
            _cameraView = null;
            _youtubeView = null;
        }

        private void UpdateModeButtons(bool isPhiloMode)
        {
        }

        private void pnlWeather_Paint(object sender, PaintEventArgs e)
        {
        }

        private void pnlBtnApps_Click(object sender, EventArgs e)
        {
            if (pnlApps.Visible)
            {
                pnlApps.Visible = false;
            }
            else
            {
                pnlApps.Visible = true;
                pnlApps.BringToFront();
            }
        }

        private void pnlBtnApps_MouseEnter(object sender, EventArgs e)
        {
            pnlBtnApps.BackgroundImage = Resources.glass_hov;
            pboxAppsIcon.Image = Resources.controls_icn_hov;
        }

        private void pnlBtnApps_MouseLeave(object sender, EventArgs e)
        {
            pnlBtnApps.BackgroundImage = Resources.glass;
            pboxAppsIcon.Image = Resources.controls_icn;
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _alertPollTimer.Stop();
            _alertPollTimer.Dispose();
            EndEmergencyAlertSequence();
        }
    }
}
