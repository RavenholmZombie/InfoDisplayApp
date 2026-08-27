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
            pnlTV = new Panel();
            pnlWeather = new Panel();
            pnlDateTime = new Panel();
            pnlTicker = new Panel();
            SuspendLayout();
            // 
            // pnlTV
            // 
            pnlTV.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlTV.BackColor = Color.Black;
            pnlTV.Location = new Point(0, 0);
            pnlTV.Name = "pnlTV";
            pnlTV.Size = new Size(837, 497);
            pnlTV.TabIndex = 0;
            // 
            // pnlWeather
            // 
            pnlWeather.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pnlWeather.BackColor = Color.Transparent;
            pnlWeather.Location = new Point(846, 12);
            pnlWeather.Name = "pnlWeather";
            pnlWeather.Size = new Size(137, 178);
            pnlWeather.TabIndex = 1;
            // 
            // pnlDateTime
            // 
            pnlDateTime.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            pnlDateTime.BackColor = Color.Transparent;
            pnlDateTime.Location = new Point(846, 440);
            pnlDateTime.Name = "pnlDateTime";
            pnlDateTime.Size = new Size(137, 137);
            pnlDateTime.TabIndex = 2;
            // 
            // pnlTicker
            // 
            pnlTicker.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlTicker.BackColor = Color.Transparent;
            pnlTicker.Location = new Point(9, 503);
            pnlTicker.Name = "pnlTicker";
            pnlTicker.Size = new Size(828, 74);
            pnlTicker.TabIndex = 3;
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            BackgroundImage = Properties.Resources.ChatGPT_Image_Aug_26__2026__07_37_12_PM;
            ClientSize = new Size(992, 589);
            Controls.Add(pnlTicker);
            Controls.Add(pnlDateTime);
            Controls.Add(pnlWeather);
            Controls.Add(pnlTV);
            DoubleBuffered = true;
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
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlTV;
        private Panel pnlWeather;
        private Panel pnlDateTime;
        private Panel pnlTicker;
    }
}
