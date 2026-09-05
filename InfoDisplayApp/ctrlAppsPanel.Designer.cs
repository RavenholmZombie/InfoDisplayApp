namespace InfoDisplayApp
{
    partial class ctrlAppsPanel
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            panel1 = new Panel();
            flowLayoutPanel1 = new FlowLayoutPanel();
            appPnlTapo = new Panel();
            lblTapo = new Label();
            icnTapo = new PictureBox();
            appPnlPhilo = new Panel();
            lblPhilo = new Label();
            icnPhilo = new PictureBox();
            appPnlYouTube = new Panel();
            lblYouTube = new Label();
            icnYouTube = new PictureBox();
            appPnlEAS = new Panel();
            lblEAS = new Label();
            icnEAS = new PictureBox();
            dbpBtnClose = new DoubleBufferedPanel();
            lblBtnClose = new Label();
            dbpBtnRestart = new DoubleBufferedPanel();
            lblBtnRestart = new Label();
            appPnlBrowser = new Panel();
            lblBrowser = new Label();
            icnBrowser = new PictureBox();
            panel1.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            appPnlTapo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)icnTapo).BeginInit();
            appPnlPhilo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)icnPhilo).BeginInit();
            appPnlYouTube.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)icnYouTube).BeginInit();
            appPnlEAS.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)icnEAS).BeginInit();
            dbpBtnClose.SuspendLayout();
            dbpBtnRestart.SuspendLayout();
            appPnlBrowser.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)icnBrowser).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel1.BackColor = SystemColors.Control;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(flowLayoutPanel1);
            panel1.ForeColor = Color.Black;
            panel1.Location = new Point(15, 61);
            panel1.Name = "panel1";
            panel1.Size = new Size(267, 220);
            panel1.TabIndex = 0;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            flowLayoutPanel1.Controls.Add(appPnlTapo);
            flowLayoutPanel1.Controls.Add(appPnlPhilo);
            flowLayoutPanel1.Controls.Add(appPnlYouTube);
            flowLayoutPanel1.Controls.Add(appPnlEAS);
            flowLayoutPanel1.Controls.Add(appPnlBrowser);
            flowLayoutPanel1.Location = new Point(0, 0);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(265, 218);
            flowLayoutPanel1.TabIndex = 0;
            // 
            // appPnlTapo
            // 
            appPnlTapo.Controls.Add(lblTapo);
            appPnlTapo.Controls.Add(icnTapo);
            appPnlTapo.Cursor = Cursors.Hand;
            appPnlTapo.Location = new Point(3, 3);
            appPnlTapo.Name = "appPnlTapo";
            appPnlTapo.Size = new Size(82, 98);
            appPnlTapo.TabIndex = 4;
            // 
            // lblTapo
            // 
            lblTapo.AutoSize = true;
            lblTapo.Location = new Point(24, 64);
            lblTapo.Name = "lblTapo";
            lblTapo.Size = new Size(33, 15);
            lblTapo.TabIndex = 1;
            lblTapo.Text = "Tapo";
            lblTapo.TextAlign = ContentAlignment.TopCenter;
            // 
            // icnTapo
            // 
            icnTapo.Image = Properties.Resources.tapo_icn;
            icnTapo.Location = new Point(17, 12);
            icnTapo.Name = "icnTapo";
            icnTapo.Size = new Size(49, 49);
            icnTapo.SizeMode = PictureBoxSizeMode.StretchImage;
            icnTapo.TabIndex = 0;
            icnTapo.TabStop = false;
            // 
            // appPnlPhilo
            // 
            appPnlPhilo.Controls.Add(lblPhilo);
            appPnlPhilo.Controls.Add(icnPhilo);
            appPnlPhilo.Cursor = Cursors.Hand;
            appPnlPhilo.Location = new Point(91, 3);
            appPnlPhilo.Name = "appPnlPhilo";
            appPnlPhilo.Size = new Size(82, 98);
            appPnlPhilo.TabIndex = 0;
            appPnlPhilo.Click += appPnlPhilo_Click;
            // 
            // lblPhilo
            // 
            lblPhilo.AutoSize = true;
            lblPhilo.Location = new Point(24, 64);
            lblPhilo.Name = "lblPhilo";
            lblPhilo.Size = new Size(34, 15);
            lblPhilo.TabIndex = 1;
            lblPhilo.Text = "Philo";
            // 
            // icnPhilo
            // 
            icnPhilo.Image = Properties.Resources.philo_icn;
            icnPhilo.Location = new Point(17, 12);
            icnPhilo.Name = "icnPhilo";
            icnPhilo.Size = new Size(49, 49);
            icnPhilo.SizeMode = PictureBoxSizeMode.StretchImage;
            icnPhilo.TabIndex = 0;
            icnPhilo.TabStop = false;
            // 
            // appPnlYouTube
            // 
            appPnlYouTube.Controls.Add(lblYouTube);
            appPnlYouTube.Controls.Add(icnYouTube);
            appPnlYouTube.Cursor = Cursors.Hand;
            appPnlYouTube.Location = new Point(179, 3);
            appPnlYouTube.Name = "appPnlYouTube";
            appPnlYouTube.Size = new Size(82, 98);
            appPnlYouTube.TabIndex = 2;
            appPnlYouTube.Click += appPnlYouTube_Click;
            // 
            // lblYouTube
            // 
            lblYouTube.AutoSize = true;
            lblYouTube.Location = new Point(14, 64);
            lblYouTube.Name = "lblYouTube";
            lblYouTube.Size = new Size(54, 15);
            lblYouTube.TabIndex = 1;
            lblYouTube.Text = "YouTube";
            // 
            // icnYouTube
            // 
            icnYouTube.Image = Properties.Resources.youtube_icn;
            icnYouTube.Location = new Point(17, 12);
            icnYouTube.Name = "icnYouTube";
            icnYouTube.Size = new Size(49, 49);
            icnYouTube.SizeMode = PictureBoxSizeMode.StretchImage;
            icnYouTube.TabIndex = 0;
            icnYouTube.TabStop = false;
            // 
            // appPnlEAS
            // 
            appPnlEAS.Controls.Add(lblEAS);
            appPnlEAS.Controls.Add(icnEAS);
            appPnlEAS.Cursor = Cursors.Hand;
            appPnlEAS.Location = new Point(3, 107);
            appPnlEAS.Name = "appPnlEAS";
            appPnlEAS.Size = new Size(82, 98);
            appPnlEAS.TabIndex = 3;
            appPnlEAS.Click += appPnlTapo_Click;
            // 
            // lblEAS
            // 
            lblEAS.AutoSize = true;
            lblEAS.Location = new Point(7, 64);
            lblEAS.Name = "lblEAS";
            lblEAS.Size = new Size(69, 15);
            lblEAS.TabIndex = 1;
            lblEAS.Text = "TEST ALERT";
            lblEAS.TextAlign = ContentAlignment.TopCenter;
            // 
            // icnEAS
            // 
            icnEAS.Image = Properties.Resources.alert_icn;
            icnEAS.Location = new Point(17, 12);
            icnEAS.Name = "icnEAS";
            icnEAS.Size = new Size(49, 49);
            icnEAS.SizeMode = PictureBoxSizeMode.StretchImage;
            icnEAS.TabIndex = 0;
            icnEAS.TabStop = false;
            // 
            // dbpBtnClose
            // 
            dbpBtnClose.BackgroundImage = Properties.Resources.glass_btn_close_norm;
            dbpBtnClose.BackgroundImageLayout = ImageLayout.Stretch;
            dbpBtnClose.Controls.Add(lblBtnClose);
            dbpBtnClose.Location = new Point(15, 18);
            dbpBtnClose.Name = "dbpBtnClose";
            dbpBtnClose.Size = new Size(125, 24);
            dbpBtnClose.TabIndex = 3;
            dbpBtnClose.Click += dbpBtnClose_Click;
            dbpBtnClose.MouseEnter += dbpBtnClose_MouseEnter;
            dbpBtnClose.MouseLeave += dbpBtnClose_MouseLeave;
            // 
            // lblBtnClose
            // 
            lblBtnClose.AutoSize = true;
            lblBtnClose.ForeColor = Color.White;
            lblBtnClose.Location = new Point(26, 5);
            lblBtnClose.Name = "lblBtnClose";
            lblBtnClose.Size = new Size(89, 15);
            lblBtnClose.TabIndex = 0;
            lblBtnClose.Text = "Quit InfoScreen";
            // 
            // dbpBtnRestart
            // 
            dbpBtnRestart.BackgroundImage = Properties.Resources.glass_btn_restart_norm;
            dbpBtnRestart.BackgroundImageLayout = ImageLayout.Stretch;
            dbpBtnRestart.Controls.Add(lblBtnRestart);
            dbpBtnRestart.Location = new Point(156, 18);
            dbpBtnRestart.Name = "dbpBtnRestart";
            dbpBtnRestart.Size = new Size(125, 24);
            dbpBtnRestart.TabIndex = 4;
            dbpBtnRestart.Click += dbpBtnRestart_Click;
            dbpBtnRestart.MouseEnter += dbpBtnRestart_MouseEnter;
            dbpBtnRestart.MouseLeave += dbpBtnRestart_MouseLeave;
            // 
            // lblBtnRestart
            // 
            lblBtnRestart.AutoSize = true;
            lblBtnRestart.ForeColor = Color.White;
            lblBtnRestart.Location = new Point(21, 5);
            lblBtnRestart.Name = "lblBtnRestart";
            lblBtnRestart.Size = new Size(102, 15);
            lblBtnRestart.TabIndex = 1;
            lblBtnRestart.Text = "Restart InfoScreen";
            // 
            // appPnlBrowser
            // 
            appPnlBrowser.Controls.Add(lblBrowser);
            appPnlBrowser.Controls.Add(icnBrowser);
            appPnlBrowser.Cursor = Cursors.Hand;
            appPnlBrowser.Location = new Point(91, 107);
            appPnlBrowser.Name = "appPnlBrowser";
            appPnlBrowser.Size = new Size(82, 98);
            appPnlBrowser.TabIndex = 4;
            appPnlBrowser.Click += appPnlBrowser_Click;
            // 
            // lblBrowser
            // 
            lblBrowser.AutoSize = true;
            lblBrowser.Location = new Point(17, 64);
            lblBrowser.Name = "lblBrowser";
            lblBrowser.Size = new Size(49, 15);
            lblBrowser.TabIndex = 1;
            lblBrowser.Text = "Browser";
            lblBrowser.TextAlign = ContentAlignment.TopCenter;
            // 
            // icnBrowser
            // 
            icnBrowser.Image = Properties.Resources.browser_icn;
            icnBrowser.Location = new Point(17, 12);
            icnBrowser.Name = "icnBrowser";
            icnBrowser.Size = new Size(49, 49);
            icnBrowser.SizeMode = PictureBoxSizeMode.StretchImage;
            icnBrowser.TabIndex = 0;
            icnBrowser.TabStop = false;
            // 
            // ctrlAppsPanel
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Transparent;
            BackgroundImage = Properties.Resources.glass_bl;
            BackgroundImageLayout = ImageLayout.Stretch;
            Controls.Add(dbpBtnRestart);
            Controls.Add(dbpBtnClose);
            Controls.Add(panel1);
            DoubleBuffered = true;
            Name = "ctrlAppsPanel";
            Size = new Size(297, 304);
            Load += ctrlAppsPanel_Load;
            panel1.ResumeLayout(false);
            flowLayoutPanel1.ResumeLayout(false);
            appPnlTapo.ResumeLayout(false);
            appPnlTapo.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)icnTapo).EndInit();
            appPnlPhilo.ResumeLayout(false);
            appPnlPhilo.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)icnPhilo).EndInit();
            appPnlYouTube.ResumeLayout(false);
            appPnlYouTube.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)icnYouTube).EndInit();
            appPnlEAS.ResumeLayout(false);
            appPnlEAS.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)icnEAS).EndInit();
            dbpBtnClose.ResumeLayout(false);
            dbpBtnClose.PerformLayout();
            dbpBtnRestart.ResumeLayout(false);
            dbpBtnRestart.PerformLayout();
            appPnlBrowser.ResumeLayout(false);
            appPnlBrowser.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)icnBrowser).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private FlowLayoutPanel flowLayoutPanel1;
        private Panel appPnlPhilo;
        private PictureBox icnPhilo;
        private Label lblPhilo;
        private Panel appPnlYouTube;
        private Label lblYouTube;
        private PictureBox icnYouTube;
        private Panel appPnlEAS;
        private Label lblEAS;
        private PictureBox icnEAS;
        private Panel appPnlTapo;
        private Label lblTapo;
        private PictureBox icnTapo;
        private DoubleBufferedPanel dbpBtnClose;
        private DoubleBufferedPanel dbpBtnRestart;
        private Label lblBtnClose;
        private Label lblBtnRestart;
        private Panel appPnlBrowser;
        private Label lblBrowser;
        private PictureBox icnBrowser;
    }
}
