using InfoDisplayApp.Properties;
using System.Diagnostics;

namespace InfoDisplayApp
{
    public partial class frmMain : Form
    {
        private enum DisplaySource
        {
            Philo,
            YouTube,
            Camera
        }

        private ctrlCameras? _cameraView;
        private ExternalBrowserController? _browserController;
        private ctrlAppsPanel? _appsPanel;
        private DisplaySource _activeSource = DisplaySource.Philo;

        private readonly Random _random = new Random();
        private readonly System.Windows.Forms.Timer _colorTimer = new System.Windows.Forms.Timer();
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

            pboxAppsIcon.MouseEnter += pnlBtnApps_MouseEnter;
            pboxAppsIcon.MouseLeave += pnlBtnApps_MouseLeave;
            pboxAppsIcon.Click += pnlBtnApps_Click;

            FormClosing += frmMain_FormClosing;
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
            if(mode == "normal")
            {
                ctrlTicker ctrlTicker = new ctrlTicker
                {
                    Dock = DockStyle.Fill
                };
                pnlTicker.Controls.Add(ctrlTicker);
                ctrlTicker.BringToFront();
                tickerMode = "normal";
            }
            else
            {
                ctrlEmergencyTicker ctrlEmergencyTicker = new ctrlEmergencyTicker();
                pnlTicker.Controls.Add(ctrlEmergencyTicker);
                ctrlEmergencyTicker.Dock = DockStyle.Fill;
                ctrlEmergencyTicker.BringToFront();
                tickerMode = "EAS";
            }
        }

        private async void frmMain_Load(object sender, EventArgs e)
        {
            // -----------------------------
            // CAMERA
            // -----------------------------

            _cameraView = new ctrlCameras
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            pnlTV.Controls.Add(_cameraView);

            // -----------------------------
            // EXTERNAL BROWSER VIEWERS
            // -----------------------------
            // Philo and YouTube run in separate normal Edge app windows. They
            // are positioned over pnlTV and controlled through localhost CDP.
            // This keeps DRM/video playback out of WebView2's embedded media
            // pipeline while retaining instant source switching.

            _browserController = new ExternalBrowserController(this, pnlTV);

            // -----------------------------
            // DATE / TIME
            // -----------------------------

            ctrlTimeDate ctrlTimeDate = new ctrlTimeDate
            {
                Dock = DockStyle.Fill
            };
            pnlDateTime.Controls.Add(ctrlTimeDate);

            // -----------------------------
            // TEXT TICKER
            // -----------------------------
            ctrlTicker ctrlTicker = new ctrlTicker
            {
                Dock = DockStyle.Fill
            };
            pnlTicker.Controls.Add(ctrlTicker);

            // -----------------------------
            // WEATHER
            // -----------------------------

            ctrlWeather ctrlWeather = new ctrlWeather
            {
                Dock = DockStyle.Fill
            };

            pnlWeather.Controls.Add(ctrlWeather);

            // -----------------------------
            // APP PANEL
            // -----------------------------
            _appsPanel = new ctrlAppsPanel
            {
                Dock = DockStyle.Fill
            };

            pnlApps.Controls.Add(_appsPanel);
            pnlApps.Visible = false;

            UpdateModeButtons(true);

            try
            {
                await _browserController.InitializeAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"External browser viewer initialization failed: {ex}");
                AppMessages.Error(
                    "Philo/YouTube browser viewer could not be started.",
                    ex);
            }
        }

        public async void ShowPhiloMode()
        {
            if (_cameraView == null || _browserController == null)
                return;

            _activeSource = DisplaySource.Philo;

            _cameraView.SetMuted(true);
            _cameraView.StopCamera();
            _cameraView.Visible = false;

            try
            {
                await _browserController.ShowPhiloAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to switch to Philo: {ex}");
                AppMessages.Error("Unable to switch to Philo.", ex);
            }

            pnlApps.Hide();
            UpdateModeButtons(true);
        }

        public async void ShowCameraMode()
        {
            if (_cameraView == null)
                return;

            _activeSource = DisplaySource.Camera;

            if (_browserController != null)
            {
                try
                {
                    await _browserController.HideAllAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unable to hide browser viewers: {ex.Message}");
                }
            }

            _cameraView.Visible = true;
            _cameraView.BringToFront();
            _cameraView.SetMuted(false);
            _cameraView.StartCamera();
            pnlApps.Hide();

            UpdateModeButtons(false);
        }

        public async void ShowYouTubeMode()
        {
            if (_cameraView == null || _browserController == null)
                return;

            _activeSource = DisplaySource.YouTube;

            _cameraView.SetMuted(true);
            _cameraView.StopCamera();
            _cameraView.Visible = false;

            try
            {
                await _browserController.ShowYouTubeAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to switch to YouTube: {ex}");
                AppMessages.Error("Unable to switch to YouTube.", ex);
            }

            pnlApps.Hide();
            UpdateModeButtons(true);
        }

        private void UpdateModeButtons(bool isPhiloMode)
        {
            //btnPhiloMode.Enabled = !isPhiloMode;
            //btnCameraMode.Enabled = isPhiloMode;
            //btnCameraMode.Enabled = _cameraView.IsConfigured;
        }

        private void pnlWeather_Paint(object sender, PaintEventArgs e)
        {
        }

        private async void pnlBtnApps_Click(object sender, EventArgs e)
        {
            if (pnlApps.Visible)
            {
                pnlApps.Visible = false;
                await RestoreActiveSourceAsync();
            }
            else
            {
                if (_activeSource != DisplaySource.Camera && _browserController != null)
                    await _browserController.HideAllAsync();

                pnlApps.Visible = true;
                pnlApps.BringToFront();
            }
        }

        private async Task RestoreActiveSourceAsync()
        {
            if (_browserController == null)
                return;

            try
            {
                switch (_activeSource)
                {
                    case DisplaySource.Philo:
                        await _browserController.ShowPhiloAsync();
                        break;

                    case DisplaySource.YouTube:
                        await _browserController.ShowYouTubeAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to restore browser viewer: {ex.Message}");
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

        private void frmMain_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Dispose the external browser controller before the top-level
            // InfoDisplay window disappears so Philo/YouTube cannot be orphaned.
            _browserController?.Dispose();
            _browserController = null;
        }
    }
}
