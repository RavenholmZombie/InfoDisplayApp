using InfoDisplayApp.Properties;
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

            // EAS
            icnEAS.Click += appPnlEAS_Click;
            lblEAS.Click += appPnlEAS_Click;

            // Tapo
            icnTapo.Click += appPnlTapo_Click;
            lblTapo.Click += appPnlTapo_Click;

            // Quit Button
            lblBtnClose.Click += dbpBtnClose_Click;
            lblBtnClose.MouseEnter += dbpBtnClose_MouseEnter;
            lblBtnClose.MouseLeave += dbpBtnClose_MouseLeave;
            lblBtnClose.Cursor = Cursors.Hand;

            // Restart Button
            lblBtnRestart.Click += dbpBtnRestart_Click;
            lblBtnRestart.MouseEnter += dbpBtnRestart_MouseEnter;
            lblBtnRestart.MouseLeave += dbpBtnRestart_MouseLeave;
            lblBtnRestart.Cursor = Cursors.Hand;
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

        private void appPnlEAS_Click(object sender, EventArgs e)
        {
            SendAppChange(sender, e, "EAS");
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
                else if (appName == "EAS")
                {
                    if (mainForm.tickerMode == "EAS")
                    {
                        mainForm.ToggleTickerMode("Normal");
                        mainForm.tickerMode = "normal";
                    }
                    else
                    {
                        mainForm.ToggleTickerMode("EAS");
                        mainForm.tickerMode = "EAS";
                    }
                }
                else if (appName == "browser")
                {
                    mainForm.ShowBrowserMode();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending app change to main form: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ctrlAppsPanel_Load(object sender, EventArgs e)
        {

        }

        private void dbpBtnClose_MouseEnter(object sender, EventArgs e)
        {
            dbpBtnClose.BackgroundImage = Resources.glass_btn_close_hover;
        }

        private void dbpBtnClose_MouseLeave(object sender, EventArgs e)
        {
            dbpBtnClose.BackgroundImage = Resources.glass_btn_close_norm;
        }

        private void dbpBtnClose_Click(object sender, EventArgs e)
        {
            if (AppMessages.AskYesNo("Do you wish to close InfoScreen?"))
            {
                frmClosing frmClosing = new frmClosing();
                frmClosing.setRestarting(false);
                frmClosing.ShowDialog(this);
            }
        }

        private void dbpBtnRestart_MouseEnter(object sender, EventArgs e)
        {
            dbpBtnRestart.BackgroundImage = Resources.glass_btn_restart_hover;
        }

        private void dbpBtnRestart_MouseLeave(object sender, EventArgs e)
        {
            dbpBtnRestart.BackgroundImage = Resources.glass_btn_restart_norm;
        }

        private void dbpBtnRestart_Click(object sender, EventArgs e)
        {
            if (AppMessages.AskYesNo("Do you wish to restart InfoScreen?"))
            {
                frmClosing frmClosing = new frmClosing();
                frmClosing.setRestarting(true);
                frmClosing.ShowDialog(this);
            }
        }

        private void appPnlBrowser_Click(object sender, EventArgs e)
        {
            SendAppChange(sender, e, "browser");
        }
    }
}
