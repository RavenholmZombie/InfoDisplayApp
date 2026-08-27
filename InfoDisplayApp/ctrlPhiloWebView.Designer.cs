namespace InfoDisplayApp
{
    partial class ctrlPhiloWebView
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
            wvPhilo = new Microsoft.Web.WebView2.WinForms.WebView2();
            ((System.ComponentModel.ISupportInitialize)wvPhilo).BeginInit();
            SuspendLayout();
            // 
            // wvPhilo
            // 
            wvPhilo.AllowExternalDrop = true;
            wvPhilo.CreationProperties = null;
            wvPhilo.DefaultBackgroundColor = Color.White;
            wvPhilo.Dock = DockStyle.Fill;
            wvPhilo.Location = new Point(0, 0);
            wvPhilo.Name = "wvPhilo";
            wvPhilo.Size = new Size(419, 311);
            wvPhilo.Source = new Uri("https://www.philo.com/player/mytv", UriKind.Absolute);
            wvPhilo.TabIndex = 0;
            wvPhilo.ZoomFactor = 1D;
            // 
            // ctrlPhiloWebView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(wvPhilo);
            Name = "ctrlPhiloWebView";
            Size = new Size(419, 311);
            ((System.ComponentModel.ISupportInitialize)wvPhilo).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Microsoft.Web.WebView2.WinForms.WebView2 wvPhilo;
    }
}
