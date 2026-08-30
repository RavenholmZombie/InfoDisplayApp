using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace InfoDisplayApp
{
    public partial class ctrlAppsPanel : UserControl
    {
        public ctrlAppsPanel()
        {
            InitializeComponent();

            // Philo
            icnPhilo.Click += appPnlPhilo_Click;
            lblPhilo.Click += appPnlPhilo_Click;

            // YouTube
            icnYouTube.Click += appPnlYouTube_Click;
            lblYouTube.Click += appPnlYouTube_Click;

            // Tapo
            icnTapo.Click += appPnlTapo_Click;
            lblTapo.Click += appPnlTapo_Click;
        }

        private void appPnlPhilo_Click(object sender, EventArgs e)
        {
            SendAppChange(sender, e, "Philo");
        }
        private void appPnlYouTube_Click(object sender, EventArgs e)
        {
            SendAppChange(sender, e, "YouTube");
        }

        private void appPnlTapo_Click(object sender, EventArgs e)
        {
            SendAppChange(sender, e, "Tapo");
        }

        private void SendAppChange(object sender, EventArgs e, String appName)
        {
            frmMain? mainForm = Application.OpenForms.OfType<frmMain>().FirstOrDefault();

            try
            {
                if (appName == "Philo")
                {
                    mainForm.ShowPhiloMode();
                }
                else if (appName == "YouTube")
                {
                    mainForm.ShowYouTubeMode();
                }
                else if (appName == "Tapo")
                {
                    mainForm.ShowCameraMode();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending app change to main form: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
