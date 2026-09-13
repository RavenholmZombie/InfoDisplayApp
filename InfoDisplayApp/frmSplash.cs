using System;
using System.Windows.Forms;

namespace InfoDisplayApp
{
    public partial class frmSplash : Form
    {
        public frmSplash()
        {
            InitializeComponent();
            Opacity = 0;
        }

        public void SetStartupStatus(string message, int progress)
        {
            if (IsDisposed)
                return;

            progress = Math.Clamp(progress, pBarMain.Minimum, pBarMain.Maximum);
            lblStatus.Text = message;
            pBarMain.Value = progress;
            lblStatus.Refresh();
            pBarMain.Refresh();
        }

        private void frmSplash_Load(object sender, EventArgs e)
        {
            SetStartupStatus("Starting Info Display...", 10);
            fadeTimer.Start();
        }

        private void fadeTimer_Tick(object sender, EventArgs e)
        {
            Opacity += 0.2;
            if (Opacity >= 1.0)
            {
                Opacity = 1.0;
                fadeTimer.Stop();
            }
        }
    }
}
