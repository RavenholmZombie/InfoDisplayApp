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
    public partial class ctrlEmergencyTicker : UserControl
    {
        public ctrlEmergencyTicker()
        {
            InitializeComponent();
        }

        private void ctrlEmergencyTicker_Load(object sender, EventArgs e)
        {
            using (var stream = Resources.alarm)
            using (var player = new System.Media.SoundPlayer(stream))
            {
                player.Play();
            }
        }
    }
}
