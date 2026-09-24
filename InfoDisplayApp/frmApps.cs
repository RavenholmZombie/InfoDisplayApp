namespace InfoDisplayApp
{
    /// <summary>
    /// Top-level host for the Apps UI. Keeping this control in a separate HWND
    /// prevents it from competing with WebView2 and LibVLC child windows for
    /// painting and z-order inside frmMain.
    /// </summary>
    public sealed class frmApps : Form
    {
        public frmApps(Control appsContent)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            MinimizeBox = false;
            MaximizeBox = false;
            ControlBox = false;

            appsContent.Dock = DockStyle.Fill;
            Controls.Add(appsContent);
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_TOOLWINDOW = 0x00000080;
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOOLWINDOW;
                return cp;
            }
        }
    }
}
