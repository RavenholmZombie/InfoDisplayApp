namespace InfoDisplayApp.Properties
{
    partial class ctrlTicker
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
            panel1 = new DoubleBufferedPanel();
            lblTextTicker = new Label();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel1.BackgroundImage = Resources.glass_bl;
            panel1.Controls.Add(lblTextTicker);
            panel1.Location = new Point(11, 13);
            panel1.Name = "panel1";
            panel1.Size = new Size(582, 57);
            panel1.TabIndex = 0;
            // 
            // lblTextTicker
            // 
            lblTextTicker.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblTextTicker.BackColor = Color.Transparent;
            lblTextTicker.Font = new Font("Segoe UI", 30F);
            lblTextTicker.ForeColor = Color.White;
            lblTextTicker.ImageAlign = ContentAlignment.MiddleRight;
            lblTextTicker.Location = new Point(0, 0);
            lblTextTicker.Name = "lblTextTicker";
            lblTextTicker.Size = new Size(582, 57);
            lblTextTicker.TabIndex = 0;
            lblTextTicker.TextAlign = ContentAlignment.TopRight;
            // 
            // ctrlTicker
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Transparent;
            BackgroundImage = Resources.glass_long;
            BackgroundImageLayout = ImageLayout.Stretch;
            Controls.Add(panel1);
            DoubleBuffered = true;
            ForeColor = Color.White;
            Name = "ctrlTicker";
            Size = new Size(605, 82);
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private DoubleBufferedPanel panel1;
        private Label lblTextTicker;
    }
}
