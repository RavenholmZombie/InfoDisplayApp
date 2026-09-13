using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
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

        private void frmSplash_Load(object sender, EventArgs e)
        {
            fadeTimer.Start();
        }

        private void fadeTimer_Tick(object sender, EventArgs e)
        {
            Opacity += 0.2;
            if (Opacity >= 1.0) { Opacity = 1.0; fadeTimer.Stop(); }
        }
    }
}
