namespace InfoDisplayApp
{
    partial class ctrlWeather
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
            pictureBox1 = new PictureBox();
            lblTown = new Label();
            label1 = new Label();
            label2 = new Label();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.sunny_icon_23522;
            pictureBox1.Location = new Point(42, 16);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(53, 53);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // lblTown
            // 
            lblTown.AutoSize = true;
            lblTown.Font = new Font("Segoe UI Variable Display Semib", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTown.Location = new Point(24, 76);
            lblTown.Name = "lblTown";
            lblTown.Size = new Size(89, 17);
            lblTown.TabIndex = 1;
            lblTown.Text = "Princeton, ME";
            // 
            // label1
            // 
            label1.Location = new Point(18, 95);
            label1.Name = "label1";
            label1.Size = new Size(100, 18);
            label1.TabIndex = 2;
            label1.Text = "Sunny";
            label1.TextAlign = ContentAlignment.TopCenter;
            // 
            // label2
            // 
            label2.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.Location = new Point(18, 114);
            label2.Name = "label2";
            label2.Size = new Size(100, 34);
            label2.TabIndex = 3;
            label2.Text = "67°F";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // ctrlWeather
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Transparent;
            BackgroundImage = Properties.Resources.glass;
            BackgroundImageLayout = ImageLayout.Stretch;
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(lblTown);
            Controls.Add(pictureBox1);
            ForeColor = Color.White;
            Name = "ctrlWeather";
            Size = new Size(137, 178);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PictureBox pictureBox1;
        private Label lblTown;
        private Label label1;
        private Label label2;
    }
}
