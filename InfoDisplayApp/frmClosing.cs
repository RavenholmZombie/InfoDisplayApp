using InfoDisplayApp.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Media;
using System.Text;
using System.Windows.Forms;

namespace InfoDisplayApp
{
    public partial class frmClosing : Form
    {
        private int _countdown = 7; // Seconds
        private bool _isRestarting = false; // Tells the form whether to restart or exit the application.
        private SoundPlayer? _exitSoundPlayer;
        public frmClosing()
        {
            InitializeComponent();
        }

        private void frmClosing_Load(object sender, EventArgs e)
        {
            _exitSoundPlayer?.Dispose();
            _exitSoundPlayer = new SoundPlayer(Resources.sfx_exit);
            _exitSoundPlayer.Load();
            _exitSoundPlayer.Play();
            actionTimer.Start();
            Cursor = Cursors.WaitCursor;

            // Label handling
            if (_isRestarting)
            {
                // Case - Restarting
                statusLabel.Text = "We'll be right back!\nRestarting InfoScreen...";
            }
            else
            {
                // Case - Exiting
                statusLabel.Text = "See you later!\nClosing to Windows...";
            }
        }

        private void actionTimer_Tick(object sender, EventArgs e)
        {
            _countdown--;
            if (_countdown <= 0)
            {
                actionTimer.Stop();
                DisposeExitSoundPlayer();

                if (_isRestarting)
                {
                    Application.Restart();

                }
                else
                {
                    Application.Exit();
                }
            }
        }

        private void DisposeExitSoundPlayer()
        {
            try { _exitSoundPlayer?.Stop(); }
            catch { }
            _exitSoundPlayer?.Dispose();
            _exitSoundPlayer = null;
        }

        public bool setRestarting(bool isRestarting)
        {
            _isRestarting = isRestarting;
            return _isRestarting;
        }
    }
}
