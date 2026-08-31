using InfoDisplayApp.Properties;

namespace InfoDisplayApp
{
    public partial class frmMain : Form
    {
        private ctrlPhiloWebView? _philoView;
        private ctrlCameras? _cameraView;
        private ctrlYouTubeWebView? _youtubeView;
        private ctrlAppsPanel? _appsPanel;

        private readonly Random _random = new Random();
        private readonly System.Windows.Forms.Timer _colorTimer = new System.Windows.Forms.Timer();

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

        private void frmMain_Load(object sender, EventArgs e)
        {
            // -----------------------------
            // EAS TEXT TICKER - WIP
            // -----------------------------
            //ctrlEmergencyTicker ctrlEmergencyTicker = new ctrlEmergencyTicker();
            //pnlTicker.Controls.Add(ctrlEmergencyTicker);
            //ctrlEmergencyTicker.Dock = DockStyle.Fill;


            // -----------------------------
            // PHILO
            // -----------------------------

            _philoView = new ctrlPhiloWebView
            {
                Dock = DockStyle.Fill,
                Visible = true
            };

            pnlTV.Controls.Add(_philoView);


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
            // YOUTUBE
            // -----------------------------

            _youtubeView = new ctrlYouTubeWebView
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            pnlTV.Controls.Add(_youtubeView);

            // -----------------------------
            // PHILO
            // -----------------------------

            // Make absolutely sure Philo owns the viewport
            _philoView.BringToFront();


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
            ctrlAppsPanel ctrlAppsPanel = new ctrlAppsPanel
            {
                Dock = DockStyle.Fill
            };

            pnlApps.Controls.Add(ctrlAppsPanel);
            pnlApps.Visible = false;

            UpdateModeButtons(true);
        }

        // =====================================================
        // PHILO MODE
        // =====================================================

        public void ShowPhiloMode()
        {
            if (_philoView == null || _cameraView == null)
                return;

            _cameraView.SetMuted(true);
            _cameraView.StopCamera();
            _cameraView.Visible = false;
            _youtubeView.SetMuted(true);
            _youtubeView.Visible = false;

            _philoView.SetMuted(false);
            _philoView.Visible = true;
            _philoView.BringToFront();
            pnlApps.Hide();

            UpdateModeButtons(true);
        }

        // =====================================================
        // CAMERA MODE
        // =====================================================

        public void ShowCameraMode()
        {
            if (_philoView == null || _cameraView == null)
                return;

            _philoView.Visible = false;
            _philoView.SetMuted(true);
            _youtubeView.Visible = false;
            _youtubeView.SetMuted(true);

            _cameraView.Visible = true;
            _cameraView.BringToFront();

            _cameraView.SetMuted(false);
            _cameraView.StartCamera();
            pnlApps.Hide();

            UpdateModeButtons(false);
        }

        // =====================================================
        // YOUTUBE MODE
        // =====================================================

        public void ShowYouTubeMode()
        {
            if (_youtubeView == null || _cameraView == null || _philoView == null)
                return;

            _cameraView.SetMuted(true);
            _cameraView.StopCamera();
            _cameraView.Visible = false;
            _philoView.SetMuted(true);

            _youtubeView.Visible = true;
            _youtubeView.SetMuted(false);
            _youtubeView.BringToFront();
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
    }
}
