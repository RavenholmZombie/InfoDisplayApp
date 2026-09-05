namespace InfoDisplayApp
{
    partial class frmBrowser
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            webView21 = new Microsoft.Web.WebView2.WinForms.WebView2();
            cTxtURL = new ReaLTaiizor.Controls.CrownTextBox();
            btnBack = new ReaLTaiizor.Controls.ForeverButton();
            btnForward = new ReaLTaiizor.Controls.ForeverButton();
            btnRefresh = new ReaLTaiizor.Controls.ForeverButton();
            btnClose = new ReaLTaiizor.Controls.ForeverButton();
            btnGo = new ReaLTaiizor.Controls.ForeverButton();
            ((System.ComponentModel.ISupportInitialize)webView21).BeginInit();
            SuspendLayout();
            // 
            // webView21
            // 
            webView21.AllowExternalDrop = true;
            webView21.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            webView21.BackColor = SystemColors.Control;
            webView21.CreationProperties = null;
            webView21.DefaultBackgroundColor = Color.White;
            webView21.Location = new Point(0, 0);
            webView21.Name = "webView21";
            webView21.Size = new Size(1344, 564);
            webView21.Source = new Uri("https://google.com", UriKind.Absolute);
            webView21.TabIndex = 0;
            webView21.ZoomFactor = 1D;
            webView21.NavigationCompleted += webView21_NavigationCompleted;
            // 
            // cTxtURL
            // 
            cTxtURL.Anchor = AnchorStyles.Bottom;
            cTxtURL.BackColor = Color.FromArgb(69, 73, 74);
            cTxtURL.BorderStyle = BorderStyle.FixedSingle;
            cTxtURL.Font = new Font("Segoe UI", 23.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            cTxtURL.ForeColor = Color.FromArgb(220, 220, 220);
            cTxtURL.Location = new Point(405, 578);
            cTxtURL.Name = "cTxtURL";
            cTxtURL.Size = new Size(516, 49);
            cTxtURL.TabIndex = 1;
            cTxtURL.KeyPress += cTxtURL_KeyPress;
            // 
            // btnBack
            // 
            btnBack.Anchor = AnchorStyles.Bottom;
            btnBack.BackColor = Color.Transparent;
            btnBack.BackgroundImage = Properties.Resources.glass;
            btnBack.BackgroundImageLayout = ImageLayout.Stretch;
            btnBack.BaseColor = Color.Transparent;
            btnBack.Font = new Font("Segoe UI", 18F);
            btnBack.ForeColor = Color.White;
            btnBack.Location = new Point(141, 578);
            btnBack.Name = "btnBack";
            btnBack.Rounded = true;
            btnBack.Size = new Size(54, 54);
            btnBack.TabIndex = 2;
            btnBack.Text = "<";
            btnBack.TextColor = Color.FromArgb(243, 243, 243);
            btnBack.Click += btnBack_Click;
            // 
            // btnForward
            // 
            btnForward.Anchor = AnchorStyles.Bottom;
            btnForward.BackColor = Color.Transparent;
            btnForward.BackgroundImage = Properties.Resources.glass;
            btnForward.BackgroundImageLayout = ImageLayout.Stretch;
            btnForward.BaseColor = Color.Transparent;
            btnForward.Font = new Font("Segoe UI", 18F);
            btnForward.ForeColor = Color.White;
            btnForward.Location = new Point(196, 577);
            btnForward.Name = "btnForward";
            btnForward.Rounded = false;
            btnForward.Size = new Size(54, 54);
            btnForward.TabIndex = 3;
            btnForward.Text = ">";
            btnForward.TextColor = Color.FromArgb(243, 243, 243);
            btnForward.Click += btnForward_Click;
            // 
            // btnRefresh
            // 
            btnRefresh.Anchor = AnchorStyles.Bottom;
            btnRefresh.BackColor = Color.Transparent;
            btnRefresh.BackgroundImage = Properties.Resources.glass;
            btnRefresh.BackgroundImageLayout = ImageLayout.Stretch;
            btnRefresh.BaseColor = Color.Transparent;
            btnRefresh.Font = new Font("Segoe UI", 18F);
            btnRefresh.ForeColor = Color.White;
            btnRefresh.Location = new Point(287, 577);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Rounded = false;
            btnRefresh.Size = new Size(112, 54);
            btnRefresh.TabIndex = 4;
            btnRefresh.Text = "Refresh";
            btnRefresh.TextColor = Color.FromArgb(243, 243, 243);
            btnRefresh.Click += btnRefresh_Click;
            // 
            // btnClose
            // 
            btnClose.Anchor = AnchorStyles.Bottom;
            btnClose.BackColor = Color.Transparent;
            btnClose.BackgroundImage = Properties.Resources.glass_btn_close_norm;
            btnClose.BackgroundImageLayout = ImageLayout.Stretch;
            btnClose.BaseColor = Color.Transparent;
            btnClose.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnClose.ForeColor = Color.White;
            btnClose.Location = new Point(1048, 577);
            btnClose.Name = "btnClose";
            btnClose.Rounded = false;
            btnClose.Size = new Size(284, 50);
            btnClose.TabIndex = 5;
            btnClose.Text = "Close Browser";
            btnClose.TextColor = Color.FromArgb(243, 243, 243);
            btnClose.Click += btnClose_Click;
            // 
            // btnGo
            // 
            btnGo.Anchor = AnchorStyles.Bottom;
            btnGo.BackColor = Color.Transparent;
            btnGo.BackgroundImage = Properties.Resources.glass;
            btnGo.BackgroundImageLayout = ImageLayout.Stretch;
            btnGo.BaseColor = Color.Transparent;
            btnGo.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnGo.ForeColor = Color.White;
            btnGo.Location = new Point(927, 577);
            btnGo.Name = "btnGo";
            btnGo.Rounded = false;
            btnGo.Size = new Size(73, 54);
            btnGo.TabIndex = 6;
            btnGo.Text = "Go";
            btnGo.TextColor = Color.FromArgb(243, 243, 243);
            btnGo.Click += btnGo_Click;
            // 
            // frmBrowser
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            BackgroundImage = Properties.Resources.glass_bl;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(1344, 643);
            Controls.Add(btnGo);
            Controls.Add(btnClose);
            Controls.Add(btnRefresh);
            Controls.Add(btnForward);
            Controls.Add(btnBack);
            Controls.Add(cTxtURL);
            Controls.Add(webView21);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmBrowser";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "frmBrowser";
            TopMost = true;
            WindowState = FormWindowState.Maximized;
            Load += frmBrowser_Load;
            ((System.ComponentModel.ISupportInitialize)webView21).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Microsoft.Web.WebView2.WinForms.WebView2 webView21;
        private ReaLTaiizor.Controls.CrownTextBox cTxtURL;
        private ReaLTaiizor.Controls.ForeverButton btnBack;
        private ReaLTaiizor.Controls.ForeverButton btnForward;
        private ReaLTaiizor.Controls.ForeverButton btnRefresh;
        private ReaLTaiizor.Controls.ForeverButton btnClose;
        private ReaLTaiizor.Controls.ForeverButton btnGo;
    }
}