namespace InfoDisplayApp
{
    partial class ctrlCameras
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
            vlcPlayer = new LibVLCSharp.WinForms.VideoView();
            ((System.ComponentModel.ISupportInitialize)vlcPlayer).BeginInit();
            SuspendLayout();
            // 
            // vlcPlayer
            // 
            vlcPlayer.BackColor = Color.Black;
            vlcPlayer.Dock = DockStyle.Fill;
            vlcPlayer.Location = new Point(0, 0);
            vlcPlayer.MediaPlayer = null;
            vlcPlayer.Name = "vlcPlayer";
            vlcPlayer.Size = new Size(680, 407);
            vlcPlayer.TabIndex = 0;
            vlcPlayer.Text = "videoView1";
            vlcPlayer.Click += vlcPlayer_Click;
            // 
            // ctrlCameras
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(vlcPlayer);
            Name = "ctrlCameras";
            Size = new Size(680, 407);
            Load += ctrlCameras_Load;
            ((System.ComponentModel.ISupportInitialize)vlcPlayer).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private LibVLCSharp.WinForms.VideoView vlcPlayer;
    }
}
