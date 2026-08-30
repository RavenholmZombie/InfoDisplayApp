namespace InfoDisplayApp
{
    partial class ctrlEmergencyTicker
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
            lblAlertText = new Label();
            label1 = new Label();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel1.BackColor = Color.DarkRed;
            panel1.Controls.Add(lblAlertText);
            panel1.Location = new Point(3, 27);
            panel1.Name = "panel1";
            panel1.Size = new Size(822, 44);
            panel1.TabIndex = 0;
            // 
            // lblAlertText
            // 
            lblAlertText.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblAlertText.Font = new Font("Cascadia Code", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblAlertText.Location = new Point(0, 0);
            lblAlertText.Name = "lblAlertText";
            lblAlertText.Size = new Size(822, 44);
            lblAlertText.TabIndex = 0;
            lblAlertText.Text = "<Alert Message Scrolls Here>";
            lblAlertText.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(1, 4);
            label1.Name = "label1";
            label1.Size = new Size(430, 20);
            label1.TabIndex = 1;
            label1.Text = "EMERGENCY ALERT SYSTEM MESSAGE - ALERT NAME HERE";
            // 
            // ctrlEmergencyTicker
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Red;
            Controls.Add(label1);
            Controls.Add(panel1);
            ForeColor = Color.White;
            Name = "ctrlEmergencyTicker";
            Size = new Size(828, 74);
            panel1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panel1;
        private Label label1;
        private Label lblAlertText;
    }
}
