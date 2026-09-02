namespace InfoDisplayApp
{
    partial class frmMessageWindow
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
            btnClose = new Button();
            doubleBufferedPanel1 = new DoubleBufferedPanel();
            pboxIcn = new PictureBox();
            doubleBufferedPanel2 = new DoubleBufferedPanel();
            rtbMessage = new RichTextBox();
            doubleBufferedPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pboxIcn).BeginInit();
            doubleBufferedPanel2.SuspendLayout();
            SuspendLayout();
            // 
            // btnClose
            // 
            btnClose.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnClose.Location = new Point(215, 275);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(120, 41);
            btnClose.TabIndex = 3;
            btnClose.Text = "Dismiss";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            // 
            // doubleBufferedPanel1
            // 
            doubleBufferedPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            doubleBufferedPanel1.BackColor = Color.White;
            doubleBufferedPanel1.BackgroundImageLayout = ImageLayout.Stretch;
            doubleBufferedPanel1.Controls.Add(pboxIcn);
            doubleBufferedPanel1.Controls.Add(doubleBufferedPanel2);
            doubleBufferedPanel1.Location = new Point(16, 17);
            doubleBufferedPanel1.Name = "doubleBufferedPanel1";
            doubleBufferedPanel1.Size = new Size(519, 243);
            doubleBufferedPanel1.TabIndex = 2;
            // 
            // pboxIcn
            // 
            pboxIcn.BackgroundImageLayout = ImageLayout.Stretch;
            pboxIcn.Location = new Point(18, 18);
            pboxIcn.Name = "pboxIcn";
            pboxIcn.Size = new Size(62, 62);
            pboxIcn.TabIndex = 1;
            pboxIcn.TabStop = false;
            // 
            // doubleBufferedPanel2
            // 
            doubleBufferedPanel2.Controls.Add(rtbMessage);
            doubleBufferedPanel2.Location = new Point(105, 18);
            doubleBufferedPanel2.Name = "doubleBufferedPanel2";
            doubleBufferedPanel2.Size = new Size(396, 207);
            doubleBufferedPanel2.TabIndex = 0;
            // 
            // rtbMessage
            // 
            rtbMessage.BackColor = Color.White;
            rtbMessage.BorderStyle = BorderStyle.None;
            rtbMessage.Dock = DockStyle.Fill;
            rtbMessage.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            rtbMessage.Location = new Point(0, 0);
            rtbMessage.Name = "rtbMessage";
            rtbMessage.ReadOnly = true;
            rtbMessage.Size = new Size(396, 207);
            rtbMessage.TabIndex = 0;
            rtbMessage.Text = "";
            // 
            // frmMessageWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            BackgroundImage = Properties.Resources.glass_bl;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(550, 332);
            Controls.Add(btnClose);
            Controls.Add(doubleBufferedPanel1);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmMessageWindow";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "frmMessageWindow";
            Load += frmMessageWindow_Load;
            doubleBufferedPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pboxIcn).EndInit();
            doubleBufferedPanel2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Button btnClose;
        private DoubleBufferedPanel doubleBufferedPanel1;
        private PictureBox pboxIcn;
        private DoubleBufferedPanel doubleBufferedPanel2;
        private RichTextBox rtbMessage;
    }
}