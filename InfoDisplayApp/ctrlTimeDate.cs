using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace InfoDisplayApp
{
    public partial class ctrlTimeDate : UserControl
    {
        public ctrlTimeDate()
        {
            InitializeComponent();
        }

        private void ctrlTimeDate_Load(object sender, EventArgs e)
        {
            UpdateClock();

            tmrClock.Interval = 1000;
            tmrClock.Start();

            lblDay.AutoSize = false;
            lblDay.TextAlign = ContentAlignment.MiddleCenter;

            lblDate.AutoSize = false;
            lblDate.TextAlign = ContentAlignment.TopCenter;
        }

        private void UpdateClock()
        {
            DateTime now = DateTime.Now;
            lblTime.Text = now.ToString("h:mm tt");
            lblDay.Text = now.ToString("dddd");
            lblDate.Text = now.ToString("MMMM d\r\nyyyy");
        }

        private void tmrClock_Tick(object sender, EventArgs e)
        {
            UpdateClock();
        }
    }
}
