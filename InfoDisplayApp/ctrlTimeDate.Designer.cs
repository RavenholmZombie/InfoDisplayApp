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
            components = new System.ComponentModel.Container();
            lblTime = new Label();
            lblDay = new Label();
            lblDate = new Label();
            tmrClock = new System.Windows.Forms.Timer(components);
            SuspendLayout();
            // 
            // lblTime
            // 
            lblTime.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblTime.Font = new Font("Segoe UI Variable Display Semib", 15F, FontStyle.Bold);
            lblTime.ForeColor = Color.White;
            lblTime.Location = new Point(4, 2);
            lblTime.Name = "lblTime";
            lblTime.Size = new Size(171, 30);
            lblTime.TabIndex = 0;
            lblTime.Text = "00:00 PM";
            lblTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblDay
            // 
            lblDay.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblDay.Font = new Font("Segoe UI Variable Display Semib", 10F, FontStyle.Bold);
            lblDay.ForeColor = Color.White;
            lblDay.Location = new Point(26, 30);
            lblDay.Name = "lblDay";
            lblDay.Size = new Size(128, 18);
            lblDay.TabIndex = 1;
            lblDay.Text = "Wednesday";
            lblDay.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblDate
            // 
            lblDate.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblDate.Font = new Font("Segoe UI Variable Text", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblDate.ForeColor = Color.White;
            lblDate.Location = new Point(4, 51);
            lblDate.Name = "lblDate";
            lblDate.Size = new Size(171, 24);
            lblDate.TabIndex = 2;
            lblDate.Text = "December 25 2026";
            lblDate.TextAlign = ContentAlignment.TopCenter;
            // 
            // tmrClock
            // 
            tmrClock.Tick += tmrClock_Tick;
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
            DoubleBuffered = true;
            Name = "ctrlTimeDate";
            Size = new Size(180, 82);
            Load += ctrlTimeDate_Load;
            ResumeLayout(false);
        }

        #endregion

        private Label lblTime;
        private Label lblDay;
        private Label lblDate;
        private System.Windows.Forms.Timer tmrClock;
    }
}
