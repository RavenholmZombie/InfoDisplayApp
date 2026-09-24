using InfoDisplayApp.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace InfoDisplayApp
{
    public partial class ctrlAppsPanel : UserControl
    {
        private string ShutdownLogPath =>
            Path.Combine(AppContext.BaseDirectory, "logs",
                $"InfoScreen-SHUTDOWN-{Environment.ProcessId}.log");

        private void LogShutdown(string message)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ShutdownLogPath)!);
                File.AppendAllText(ShutdownLogPath,
                    $"{DateTime.Now:O} ctrlAppsPanel {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to write shutdown diagnostics: {ex}");
            }
        }
        public ctrlAppsPanel()
        {
            InitializeComponent();

            // Philo
            icnPhilo.Click += appPnlPhilo_Click;
            lblPhilo.Click += appPnlPhilo_Click;
            lblBtnRestart.Cursor = Cursors.Hand;

            // YouTube
            icnYouTube.Click += appPnlYouTube_Click;
            lblYouTube.Click += appPnlYouTube_Click;
            lblBtnRestart.Cursor = Cursors.Hand;

            // EAS test
            icnEAS.Click += appPnlEAS_Click;
            lblEAS.Click += appPnlEAS_Click;
            lblBtnRestart.Cursor = Cursors.Hand;

            // Tapo
            icnTapo.Click += appPnlTapo_Click;
            lblTapo.Click += appPnlTapo_Click;
            lblBtnRestart.Cursor = Cursors.Hand;

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

            // Browser Button
            lblBrowser.Click += appPnlBrowser_Click;
            icnBrowser.Click += appPnlBrowser_Click;
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
            frmMain? mainForm = Application.OpenForms.OfType<frmMain>().FirstOrDefault();
            mainForm?.TriggerNationalPeriodicTest();
        }

        private void SendAppChange(object sender, EventArgs e, String appName)
        {
            frmMain? mainForm = Application.OpenForms.OfType<frmMain>().FirstOrDefault();

            if (mainForm == null)
                return;

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
                LogShutdown("Close confirmed.");
                frmMain? mainForm = Application.OpenForms.OfType<frmMain>().FirstOrDefault();
                LogShutdown("Calling PrepareForShutdown().");
                mainForm?.PrepareForShutdown();
                LogShutdown("PrepareForShutdown() returned.");

                frmClosing frmClosing = new frmClosing();
                frmClosing.setRestarting(false);
                LogShutdown("Showing frmClosing for exit.");
                frmClosing.ShowDialog(this);
                LogShutdown("frmClosing exit dialog returned.");
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
                LogShutdown("Restart confirmed.");
                frmMain? mainForm = Application.OpenForms.OfType<frmMain>().FirstOrDefault();
                LogShutdown("Calling PrepareForShutdown().");
                mainForm?.PrepareForShutdown();
                LogShutdown("PrepareForShutdown() returned.");

                frmClosing frmClosing = new frmClosing();
                frmClosing.setRestarting(true);
                LogShutdown("Showing frmClosing for restart.");
                frmClosing.ShowDialog(this);
                LogShutdown("frmClosing restart dialog returned.");
            }
        }

        private void appPnlBrowser_Click(object sender, EventArgs e)
        {
            SendAppChange(sender, e, "browser");
        }

        private void aloneButton1_Click(object sender, EventArgs e)
        {
            frmSplash frmSplash = new frmSplash();
            frmSplash.ShowDialog(this);
            frmSplash.BringToFront();
        }
    }
}
