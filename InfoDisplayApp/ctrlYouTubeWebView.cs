using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace InfoDisplayApp
{
    public partial class ctrlYouTubeWebView : UserControl
    {
        public ctrlYouTubeWebView()
        {
            InitializeComponent();
        }

        public void SetMuted(bool muted)
        {
            if(wvYouTube == null) { return; }

            if (muted)
            {
                wvYouTube.ExecuteScriptAsync("document.querySelector('video').muted = true;");
                wvYouTube.Source = new Uri("https://youtube.com");
            }
            else
            {
                wvYouTube.ExecuteScriptAsync("document.querySelector('video').muted = false;");
                wvYouTube.Source = new Uri("https://youtube.com");
            }
        }
    }

}
