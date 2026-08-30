using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace InfoDisplayApp
{
    public partial class ctrlPhiloWebView : UserControl
    {
        public ctrlPhiloWebView()
        {
            InitializeComponent();
        }

        public void SetMuted(bool muted)
        {
            if (wvPhilo == null) { return; }

            if (muted)
            {
                wvPhilo.ExecuteScriptAsync("document.querySelector('video').muted = true;");
                wvPhilo.Source = new Uri("https://www.philo.com/player/mytv");
            }
            else
            {
                wvPhilo.ExecuteScriptAsync("document.querySelector('video').muted = false;");
                wvPhilo.Source = new Uri("https://www.philo.com/player/mytv");
            }
        }
    }
}
