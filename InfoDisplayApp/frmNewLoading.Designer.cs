namespace InfoDisplayApp.Properties
{
    partial class frmNewLoading
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
            lblStatus = new Label();
            pboxSpinner = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pboxSpinner).BeginInit();
            SuspendLayout();
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.BackColor = Color.Transparent;
            lblStatus.Font = new Font("Segoe UI Variable Text Semibold", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblStatus.Location = new Point(9, 237);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(783, 33);
            lblStatus.TabIndex = 0;
            lblStatus.Text = "Loading Weather";
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pboxSpinner
            // 
            pboxSpinner.Image = Resources.spinner;
            pboxSpinner.Location = new Point(376, 186);
            pboxSpinner.Name = "pboxSpinner";
            pboxSpinner.Size = new Size(48, 48);
            pboxSpinner.SizeMode = PictureBoxSizeMode.StretchImage;
            pboxSpinner.TabIndex = 1;
            pboxSpinner.TabStop = false;
            // 
            // frmNewLoading
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(800, 450);
            Controls.Add(pboxSpinner);
            Controls.Add(lblStatus);
            ForeColor = Color.White;
            FormBorderStyle = FormBorderStyle.None;
            Name = "frmNewLoading";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "frmNewLoading";
            TopMost = true;
            WindowState = FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)pboxSpinner).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Label lblStatus;
        private PictureBox pboxSpinner;
    }
}