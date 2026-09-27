namespace InfoDisplayApp.Properties
{
    public partial class frmNewLoading : Form
    {
        public frmNewLoading()
        {
            InitializeComponent();
            Opacity = 1.0;
        }

        public void SetStartupStatus(string step)
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(() => SetStartupStatus(step));
                return;
            }

            lblStatus.Text = $"Loading {step}";
        }
    }
}
