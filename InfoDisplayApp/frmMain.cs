namespace InfoDisplayApp
{
    public partial class frmMain : Form
    {
        private ctrlPhiloWebView? _philoView;
        private ctrlCameras? _cameraView;

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
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
            btnPhiloMode.Enabled = !isPhiloMode;
            btnCameraMode.Enabled = isPhiloMode;
        }
    }
}
