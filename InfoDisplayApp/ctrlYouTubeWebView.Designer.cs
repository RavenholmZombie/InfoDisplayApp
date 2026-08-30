namespace InfoDisplayApp
{
    partial class ctrlYouTubeWebView
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
            wvYouTube = new Microsoft.Web.WebView2.WinForms.WebView2();
            ((System.ComponentModel.ISupportInitialize)wvYouTube).BeginInit();
            SuspendLayout();
            // 
            // wvYouTube
            // 
            wvYouTube.AllowExternalDrop = true;
            wvYouTube.CreationProperties = null;
            wvYouTube.DefaultBackgroundColor = Color.White;
            wvYouTube.Dock = DockStyle.Fill;
            wvYouTube.Location = new Point(0, 0);
            wvYouTube.Name = "wvYouTube";
            wvYouTube.Size = new Size(299, 226);
            wvYouTube.Source = new Uri("https://youtube.com", UriKind.Absolute);
            wvYouTube.TabIndex = 0;
            wvYouTube.ZoomFactor = 1D;
            // 
            // ctrlYouTubeWebView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(wvYouTube);
            Name = "ctrlYouTubeWebView";
            Size = new Size(299, 226);
            ((System.ComponentModel.ISupportInitialize)wvYouTube).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Microsoft.Web.WebView2.WinForms.WebView2 wvYouTube;
    }
}
