namespace InfoDisplayApp
{
    partial class frmClosing
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
            statusLabel = new Label();
            pictureBox1 = new PictureBox();
            actionTimer = new System.Windows.Forms.Timer(components);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // statusLabel
            // 
            statusLabel.Dock = DockStyle.Fill;
            statusLabel.Font = new Font("Segoe UI Semibold", 26F, FontStyle.Bold);
            statusLabel.Location = new Point(0, 0);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(799, 545);
            statusLabel.TabIndex = 0;
            statusLabel.Text = "See you later!\r\nClosing to Windows...";
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.Bottom;
            pictureBox1.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox1.Image = Properties.Resources.loading;
            pictureBox1.Location = new Point(369, 367);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(60, 60);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 1;
            pictureBox1.TabStop = false;
            // 
            // actionTimer
            // 
            actionTimer.Interval = 1000;
            actionTimer.Tick += actionTimer_Tick;
            // 
            // frmClosing
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(799, 545);
            Controls.Add(pictureBox1);
            Controls.Add(statusLabel);
            ForeColor = Color.White;
            FormBorderStyle = FormBorderStyle.None;
            Name = "frmClosing";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "frmClosing";
            TopMost = true;
            WindowState = FormWindowState.Maximized;
            Load += frmClosing_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Label statusLabel;
        private PictureBox pictureBox1;
        private System.Windows.Forms.Timer actionTimer;
    }
}