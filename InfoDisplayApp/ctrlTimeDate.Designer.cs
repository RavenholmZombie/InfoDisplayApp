namespace InfoDisplayApp
{
    partial class ctrlTimeDate
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
            lblTime = new Label();
            lblDay = new Label();
            lblDate = new Label();
            SuspendLayout();
            // 
            // lblTime
            // 
            lblTime.AutoSize = true;
            lblTime.Font = new Font("Segoe UI Variable Display Semib", 20F, FontStyle.Bold);
            lblTime.ForeColor = Color.White;
            lblTime.Location = new Point(4, 7);
            lblTime.Name = "lblTime";
            lblTime.Size = new Size(128, 36);
            lblTime.TabIndex = 0;
            lblTime.Text = "00:00 PM";
            lblTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblDay
            // 
            lblDay.Font = new Font("Segoe UI Variable Display Semib", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblDay.ForeColor = Color.White;
            lblDay.Location = new Point(4, 43);
            lblDay.Name = "lblDay";
            lblDay.Size = new Size(128, 29);
            lblDay.TabIndex = 1;
            lblDay.Text = "Wednesday";
            lblDay.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblDate
            // 
            lblDate.Font = new Font("Segoe UI Variable Text", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblDate.ForeColor = Color.White;
            lblDate.Location = new Point(4, 77);
            lblDate.Name = "lblDate";
            lblDate.Size = new Size(128, 48);
            lblDate.TabIndex = 2;
            lblDate.Text = "December 25\r\n2026";
            lblDate.TextAlign = ContentAlignment.TopCenter;
            // 
            // ctrlTimeDate
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Transparent;
            BackgroundImage = Properties.Resources.glass;
            BackgroundImageLayout = ImageLayout.Stretch;
            Controls.Add(lblDate);
            Controls.Add(lblDay);
            Controls.Add(lblTime);
            Name = "ctrlTimeDate";
            Size = new Size(137, 137);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblTime;
        private Label lblDay;
        private Label lblDate;
    }
}
