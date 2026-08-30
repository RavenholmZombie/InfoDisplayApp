namespace InfoDisplayApp
{
    partial class frmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pnlWeather = new Panel();
            pnlDateTime = new Panel();
            pnlTicker = new DoubleBufferedPanel();
            pnlTV = new Panel();
            pnlApps = new DoubleBufferedPanel();
            pnlBtnApps = new Panel();
            pboxAppsIcon = new PictureBox();
            pnlTV.SuspendLayout();
            pnlBtnApps.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pboxAppsIcon).BeginInit();
            SuspendLayout();
            // 
            // pnlWeather
            // 
            pnlWeather.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            pnlWeather.BackColor = Color.Transparent;
            pnlWeather.Location = new Point(771, 544);
            pnlWeather.Name = "pnlWeather";
            pnlWeather.Size = new Size(180, 82);
            pnlWeather.TabIndex = 1;
            pnlWeather.Paint += pnlWeather_Paint;
            // 
            // pnlDateTime
            // 
            pnlDateTime.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            pnlDateTime.BackColor = Color.Transparent;
            pnlDateTime.Location = new Point(957, 544);
            pnlDateTime.Name = "pnlDateTime";
            pnlDateTime.Size = new Size(137, 82);
            pnlDateTime.TabIndex = 2;
            // 
            // pnlTicker
            // 
            pnlTicker.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlTicker.BackColor = Color.Transparent;
            pnlTicker.Location = new Point(8, 544);
            pnlTicker.Name = "pnlTicker";
            pnlTicker.Size = new Size(605, 82);
            pnlTicker.TabIndex = 3;
            // 
            // pnlTV
            // 
            pnlTV.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlTV.BackColor = Color.Black;
            pnlTV.Controls.Add(pnlApps);
            pnlTV.Location = new Point(0, 0);
            pnlTV.Name = "pnlTV";
            pnlTV.Size = new Size(1103, 534);
            pnlTV.TabIndex = 0;
            // 
            // pnlApps
            // 
            pnlApps.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            pnlApps.BackColor = Color.Transparent;
            pnlApps.Location = new Point(546, 309);
            pnlApps.Name = "pnlApps";
            pnlApps.Size = new Size(297, 236);
            pnlApps.TabIndex = 0;
            // 
            // pnlBtnApps
            // 
            pnlBtnApps.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            pnlBtnApps.BackColor = Color.Transparent;
            pnlBtnApps.BackgroundImage = Properties.Resources.glass;
            pnlBtnApps.BackgroundImageLayout = ImageLayout.Stretch;
            pnlBtnApps.Controls.Add(pboxAppsIcon);
            pnlBtnApps.Location = new Point(619, 544);
            pnlBtnApps.Name = "pnlBtnApps";
            pnlBtnApps.Size = new Size(146, 82);
            pnlBtnApps.TabIndex = 4;
            pnlBtnApps.Click += pnlBtnApps_Click;
            pnlBtnApps.MouseEnter += pnlBtnApps_MouseEnter;
            pnlBtnApps.MouseLeave += pnlBtnApps_MouseLeave;
            // 
            // pboxAppsIcon
            // 
            pboxAppsIcon.Image = Properties.Resources.controls_icn;
            pboxAppsIcon.Location = new Point(48, 16);
            pboxAppsIcon.Name = "pboxAppsIcon";
            pboxAppsIcon.Size = new Size(50, 50);
            pboxAppsIcon.SizeMode = PictureBoxSizeMode.StretchImage;
            pboxAppsIcon.TabIndex = 0;
            pboxAppsIcon.TabStop = false;
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            BackgroundImage = Properties.Resources.glass_bl;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(1103, 634);
            Controls.Add(pnlBtnApps);
            Controls.Add(pnlTicker);
            Controls.Add(pnlDateTime);
            Controls.Add(pnlWeather);
            Controls.Add(pnlTV);
            DoubleBuffered = true;
            ForeColor = Color.White;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmMain";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "InfoDisplay Home";
            TopMost = true;
            WindowState = FormWindowState.Maximized;
            Load += frmMain_Load;
            pnlTV.ResumeLayout(false);
            pnlBtnApps.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pboxAppsIcon).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private Panel pnlWeather;
        private Panel pnlDateTime;
        private DoubleBufferedPanel pnlTicker;
        private Panel pnlTV;
        private Panel pnlBtnApps;
        private PictureBox pboxAppsIcon;
        private DoubleBufferedPanel pnlApps;
    }
}
