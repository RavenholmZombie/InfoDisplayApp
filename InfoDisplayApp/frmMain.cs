namespace InfoDisplayApp
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            // Load the TV content into pnlTV
            ctrlPhiloWebView ctrlPhiloWebView = new ctrlPhiloWebView();
            pnlTV.Controls.Add(ctrlPhiloWebView);
            ctrlPhiloWebView.Dock = DockStyle.Fill;

            // Load the time content into pnlDateTime
            ctrlTimeDate ctrlTimeDate = new ctrlTimeDate();
            pnlDateTime.Controls.Add(ctrlTimeDate);
            ctrlTimeDate.Dock = DockStyle.Fill;

            // Load the weather content into pnlWeather
            ctrlWeather ctrlWeather = new ctrlWeather();
            pnlWeather.Controls.Add(ctrlWeather);
            ctrlWeather.Dock = DockStyle.Fill;
        }
    }
}
