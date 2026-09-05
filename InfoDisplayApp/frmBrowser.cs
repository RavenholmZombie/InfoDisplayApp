using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace InfoDisplayApp
{
    public partial class frmBrowser : Form
    {
        public frmBrowser()
        {
            InitializeComponent();
        }

        private void frmBrowser_Load(object sender, EventArgs e)
        {
            cTxtURL.Text = webView21.Source.ToString();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            if(AppMessages.AskYesNo("Are you sure you want to close the browser?"))
            {
                webView21.Dispose();
                Close();
            }
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            string _url = "";
            if (cTxtURL.Text == null || cTxtURL.Text.Trim() == "")
            {
                AppMessages.Warning("Please enter a URL before attempting to navigate.");

            }
            else
            {
                try
                {
                    if(cTxtURL.Text.Contains("://"))
                    {
                        _url = cTxtURL.Text;
                    }
                    else
                    {
                        _url = "http://" + cTxtURL.Text;
                    }
                    webView21.Source = new Uri(_url);
                }
                catch (UriFormatException)
                {
                    AppMessages.Error("Invalid URL format entered in the browser form. Please enter a valid URL.");
                }
            }
        }

        private void cTxtURL_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                btnGo_Click(sender, e);
                e.Handled = true;
            }
        }

        private void webView21_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            cTxtURL.Text = webView21.Source.ToString();
        }

        private void btnForward_Click(object sender, EventArgs e)
        {
            try
            {
                if (webView21.CanGoForward)
                {
                    webView21.GoForward();
                }

            }
            catch (Exception ex)
            {
                AppMessages.Error($"Error navigating forward in the browser: {ex.Message}");
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            try
            {
                if (webView21.CanGoBack)
                {
                    webView21.GoBack();
                }

            }
            catch (Exception ex)
            {
                AppMessages.Error($"Error navigating back in the browser: {ex.Message}");
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            webView21.Refresh();
        }
    }
}
