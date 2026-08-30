namespace InfoDisplayApp
{
    public partial class frmMain : Form
    {
        private ctrlPhiloWebView? _philoView;
        private ctrlCameras? _cameraView;

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
            // WEATHER
            // -----------------------------

            ctrlWeather ctrlWeather = new ctrlWeather
            {
                Dock = DockStyle.Fill
            };

            pnlWeather.Controls.Add(ctrlWeather);

            UpdateModeButtons(true);
        }

        // =====================================================
        // PHILO MODE
        // =====================================================

        private void ShowPhiloMode()
        {
            if (_philoView == null || _cameraView == null)
                return;

            _cameraView.SetMuted(true);
            _cameraView.StopCamera();
            _cameraView.Visible = false;

            _philoView.Visible = true;
            _philoView.BringToFront();

            UpdateModeButtons(true);
        }

        // =====================================================
        // CAMERA MODE
        // =====================================================

        private void ShowCameraMode()
        {
            if (_philoView == null || _cameraView == null)
                return;

            _philoView.Visible = false;

            _cameraView.Visible = true;
            _cameraView.BringToFront();

            _cameraView.SetMuted(false);
            _cameraView.StartCamera();

            UpdateModeButtons(false);
        }

        // =====================================================
        // MODE BUTTONS
        // =====================================================

        private void btnPhiloMode_Click(object sender, EventArgs e)
        {
            ShowPhiloMode();
        }

        private void btnCameraMode_Click(object sender, EventArgs e)
        {
            ShowCameraMode();
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
    }
}
