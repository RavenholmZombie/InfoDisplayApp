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
            appPnlPhilo = new Panel();
            lblPhilo = new Label();
            icnPhilo = new PictureBox();
            appPnlYouTube = new Panel();
            lblYouTube = new Label();
            icnYouTube = new PictureBox();
            appPnlTapo = new Panel();
            lblTapo = new Label();
            icnTapo = new PictureBox();
            panel1.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            appPnlPhilo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)icnPhilo).BeginInit();
            appPnlYouTube.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)icnYouTube).BeginInit();
            appPnlTapo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)icnTapo).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel1.BackColor = SystemColors.Control;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(flowLayoutPanel1);
            panel1.ForeColor = Color.Black;
            panel1.Location = new Point(15, 17);
            panel1.Name = "panel1";
            panel1.Size = new Size(267, 196);
            panel1.TabIndex = 0;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            flowLayoutPanel1.Controls.Add(appPnlPhilo);
            flowLayoutPanel1.Controls.Add(appPnlYouTube);
            flowLayoutPanel1.Controls.Add(appPnlTapo);
            flowLayoutPanel1.Location = new Point(0, 0);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(265, 194);
            flowLayoutPanel1.TabIndex = 0;
            // 
            // appPnlPhilo
            // 
            appPnlPhilo.Controls.Add(lblPhilo);
            appPnlPhilo.Controls.Add(icnPhilo);
            appPnlPhilo.Cursor = Cursors.Hand;
            appPnlPhilo.Location = new Point(3, 3);
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
            appPnlYouTube.Location = new Point(91, 3);
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
            // appPnlTapo
            // 
            appPnlTapo.Controls.Add(lblTapo);
            appPnlTapo.Controls.Add(icnTapo);
            appPnlTapo.Cursor = Cursors.Hand;
            appPnlTapo.Location = new Point(179, 3);
            appPnlTapo.Name = "appPnlTapo";
            appPnlTapo.Size = new Size(82, 98);
            appPnlTapo.TabIndex = 3;
            appPnlTapo.Click += appPnlTapo_Click;
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
            // ctrlAppsPanel
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Transparent;
            BackgroundImage = Properties.Resources.glass_bl;
            BackgroundImageLayout = ImageLayout.Stretch;
            Controls.Add(panel1);
            DoubleBuffered = true;
            Name = "ctrlAppsPanel";
            Size = new Size(297, 236);
            panel1.ResumeLayout(false);
            flowLayoutPanel1.ResumeLayout(false);
            appPnlPhilo.ResumeLayout(false);
            appPnlPhilo.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)icnPhilo).EndInit();
            appPnlYouTube.ResumeLayout(false);
            appPnlYouTube.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)icnYouTube).EndInit();
            appPnlTapo.ResumeLayout(false);
            appPnlTapo.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)icnTapo).EndInit();
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
        private Panel appPnlTapo;
        private Label lblTapo;
        private PictureBox icnTapo;
    }
}
