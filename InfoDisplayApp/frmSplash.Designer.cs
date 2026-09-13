namespace InfoDisplayApp
{
    partial class frmSplash
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
            components = new System.ComponentModel.Container();
            pBarMain = new ReaLTaiizor.Controls.RibbonProgressBarCenter();
            label1 = new Label();
            pBoxIcon = new PictureBox();
            lblStatus = new Label();
            fadeTimer = new System.Windows.Forms.Timer(components);
            ((System.ComponentModel.ISupportInitialize)pBoxIcon).BeginInit();
            SuspendLayout();
            // 
            // pBarMain
            // 
            pBarMain.BackColor = Color.Transparent;
            pBarMain.BaseColor = Color.FromArgb(75, 255, 255, 255);
            pBarMain.BorderColor = Color.FromArgb(117, 120, 117);
            pBarMain.ColorA = Color.FromArgb(203, 201, 205);
            pBarMain.ColorB = Color.FromArgb(188, 186, 190);
            pBarMain.EdgeColor = Color.FromArgb(125, 97, 94, 90);
            pBarMain.ForeColor = Color.Black;
            pBarMain.HatchType = System.Drawing.Drawing2D.HatchStyle.DarkUpwardDiagonal;
            pBarMain.Location = new Point(12, 223);
            pBarMain.Maximum = 100;
            pBarMain.Name = "pBarMain";
            pBarMain.PercentageText = "%";
            pBarMain.ProgressBorderColorA = Color.FromArgb(150, 97, 94, 90);
            pBarMain.ProgressBorderColorB = Color.FromArgb(142, 107, 46);
            pBarMain.ProgressColorA = Color.FromArgb(214, 162, 68);
            pBarMain.ProgressColorB = Color.FromArgb(199, 147, 53);
            pBarMain.ProgressLineColorA = Color.FromArgb(40, 255, 255, 255);
            pBarMain.ProgressLineColorB = Color.FromArgb(20, 255, 255, 255);
            pBarMain.ShowEdge = false;
            pBarMain.ShowPercentage = false;
            pBarMain.Size = new Size(450, 23);
            pBarMain.SmoothingType = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            pBarMain.TabIndex = 0;
            pBarMain.Text = "ribbonProgressBarCenter1";
            pBarMain.Value = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = Color.Transparent;
            label1.Font = new Font("Segoe UI Semibold", 21.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.White;
            label1.Location = new Point(150, 117);
            label1.Name = "label1";
            label1.Size = new Size(174, 40);
            label1.TabIndex = 1;
            label1.Text = "Info Display";
            // 
            // pBoxIcon
            // 
            pBoxIcon.BackColor = Color.Transparent;
            pBoxIcon.BackgroundImageLayout = ImageLayout.Stretch;
            pBoxIcon.Image = Properties.Resources.info_icn;
            pBoxIcon.Location = new Point(202, 36);
            pBoxIcon.Name = "pBoxIcon";
            pBoxIcon.Size = new Size(70, 70);
            pBoxIcon.SizeMode = PictureBoxSizeMode.StretchImage;
            pBoxIcon.TabIndex = 2;
            pBoxIcon.TabStop = false;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.BackColor = Color.Transparent;
            lblStatus.Font = new Font("Segoe UI", 11.25F, FontStyle.Italic, GraphicsUnit.Point, 0);
            lblStatus.ForeColor = Color.White;
            lblStatus.Location = new Point(12, 200);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(0, 20);
            lblStatus.TabIndex = 3;
            // 
            // fadeTimer
            // 
            fadeTimer.Interval = 50;
            fadeTimer.Tick += fadeTimer_Tick;
            // 
            // frmSplash
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ControlDarkDark;
            BackgroundImage = Properties.Resources.glass_bl;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(474, 258);
            Controls.Add(lblStatus);
            Controls.Add(pBoxIcon);
            Controls.Add(label1);
            Controls.Add(pBarMain);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            Name = "frmSplash";
            Opacity = 0.7D;
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "frmSplash";
            TopMost = true;
            Load += frmSplash_Load;
            ((System.ComponentModel.ISupportInitialize)pBoxIcon).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ReaLTaiizor.Controls.RibbonProgressBarCenter pBarMain;
        private Label label1;
        private PictureBox pBoxIcon;
        private Label lblStatus;
        private System.Windows.Forms.Timer fadeTimer;
    }
}