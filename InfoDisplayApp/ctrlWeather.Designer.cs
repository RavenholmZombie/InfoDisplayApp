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
            pBoxWeatherIcon = new PictureBox();
            lblTown = new Label();
            lblCurrentCondition = new Label();
            lblTemperature = new Label();
            ((System.ComponentModel.ISupportInitialize)pBoxWeatherIcon).BeginInit();
            SuspendLayout();
            // 
            // pBoxWeatherIcon
            // 
            pBoxWeatherIcon.Image = Properties.Resources.sunny_icon_23522;
            pBoxWeatherIcon.Location = new Point(42, 16);
            pBoxWeatherIcon.Name = "pBoxWeatherIcon";
            pBoxWeatherIcon.Size = new Size(53, 53);
            pBoxWeatherIcon.SizeMode = PictureBoxSizeMode.StretchImage;
            pBoxWeatherIcon.TabIndex = 0;
            pBoxWeatherIcon.TabStop = false;
            // 
            // lblTown
            // 
            lblTown.Font = new Font("Segoe UI Variable Display Semib", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTown.Location = new Point(8, 76);
            lblTown.Name = "lblTown";
            lblTown.Size = new Size(121, 18);
            lblTown.TabIndex = 1;
            lblTown.Text = "Princeton, ME";
            lblTown.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblCurrentCondition
            // 
            lblCurrentCondition.Location = new Point(18, 95);
            lblCurrentCondition.Name = "lblCurrentCondition";
            lblCurrentCondition.Size = new Size(100, 18);
            lblCurrentCondition.TabIndex = 2;
            lblCurrentCondition.Text = "Sunny";
            lblCurrentCondition.TextAlign = ContentAlignment.TopCenter;
            // 
            // lblTemperature
            // 
            lblTemperature.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTemperature.Location = new Point(18, 114);
            lblTemperature.Name = "lblTemperature";
            lblTemperature.Size = new Size(100, 34);
            lblTemperature.TabIndex = 3;
            lblTemperature.Text = "0°F";
            lblTemperature.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // ctrlWeather
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Transparent;
            BackgroundImage = Properties.Resources.glass;
            BackgroundImageLayout = ImageLayout.Stretch;
            Controls.Add(lblTemperature);
            Controls.Add(lblCurrentCondition);
            Controls.Add(lblTown);
            Controls.Add(pBoxWeatherIcon);
            DoubleBuffered = true;
            ForeColor = Color.White;
            Name = "ctrlWeather";
            Size = new Size(137, 178);
            Load += ctrlWeather_Load;
            ((System.ComponentModel.ISupportInitialize)pBoxWeatherIcon).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox pBoxWeatherIcon;
        private Label lblTown;
        private Label lblCurrentCondition;
        private Label lblTemperature;
    }
}
