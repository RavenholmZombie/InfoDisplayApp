using System.Diagnostics;

namespace InfoDisplayApp.Experiments
{
    /// <summary>
    /// Standalone test harness that intentionally resembles frmMain: a large
    /// browser/TV surface on top and a persistent information/control bar along
    /// the bottom. The external Firefox window is constrained to pnlBrowserSurface
    /// so its actual viewport ends before the bottom bar begins.
    /// </summary>
    internal sealed class frmFirefoxKioskExperiment : Form
    {
        private readonly Panel pnlBrowserSurface = new();
        private readonly Panel pnlBottomBar = new();
        private readonly Panel pnlStatus = new();
        private readonly Panel pnlControls = new();
        private readonly Panel pnlClock = new();
        private readonly Label lblViewportHint = new();
        private readonly Label lblStatus = new();
        private readonly Label lblClock = new();
        private readonly Button btnPhilo = new();
        private readonly Button btnYouTube = new();
        private readonly Button btnExample = new();
        private readonly Button btnStop = new();
        private readonly Button btnClose = new();
        private readonly System.Windows.Forms.Timer _clockTimer = new();
        private readonly FirefoxKioskController _firefox;

        private const string PhiloUrl = "https://www.philo.com/player/mytv";
        private const string YouTubeUrl = "https://www.youtube.com/";
        private const string ExampleUrl = "https://example.com/";

        public frmFirefoxKioskExperiment()
        {
            Text = "InfoDisplay Firefox Kiosk Experiment";
            BackColor = Color.Black;
            ForeColor = Color.White;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            TopMost = true;
            ShowIcon = false;
            DoubleBuffered = true;

            BuildLayout();

            _firefox = new FirefoxKioskController(GetBrowserScreenRectangle);
            _firefox.StatusChanged += Firefox_StatusChanged;

            _clockTimer.Interval = 1000;
            _clockTimer.Tick += (_, _) => UpdateClock();
            _clockTimer.Start();
            UpdateClock();

            Shown += (_, _) =>
            {
                lblStatus.Text =
                    "Ready. Launch a site to test whether Firefox kiosk mode stays constrained above this bar.";
            };

            Resize += (_, _) => BeginInvoke(new Action(_firefox.ReapplyBounds));
            Move += (_, _) => BeginInvoke(new Action(_firefox.ReapplyBounds));
            FormClosing += FrmFirefoxKioskExperiment_FormClosing;
        }

        private void BuildLayout()
        {
            // Browser/TV area: equivalent role to frmMain.pnlTV.
            pnlBrowserSurface.Dock = DockStyle.Fill;
            pnlBrowserSurface.BackColor = Color.Black;
            pnlBrowserSurface.Padding = new Padding(0);

            lblViewportHint.AutoSize = false;
            lblViewportHint.Dock = DockStyle.Fill;
            lblViewportHint.TextAlign = ContentAlignment.MiddleCenter;
            lblViewportHint.ForeColor = Color.DimGray;
            lblViewportHint.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblViewportHint.Text =
                "FIREFOX KIOSK VIEWPORT\r\n\r\n" +
                "The external browser window should occupy exactly this black area.\r\n" +
                "Its page viewport should never extend underneath the information bar.";
            pnlBrowserSurface.Controls.Add(lblViewportHint);

            // Persistent bottom bar: same basic relationship as frmMain's ticker,
            // apps, weather and date/time strip, but simplified for this test.
            pnlBottomBar.Dock = DockStyle.Bottom;
            pnlBottomBar.Height = 100;
            pnlBottomBar.BackColor = Color.FromArgb(28, 42, 61);
            pnlBottomBar.Padding = new Padding(8);

            pnlStatus.Dock = DockStyle.Fill;
            pnlStatus.BackColor = Color.FromArgb(36, 55, 79);
            pnlStatus.Padding = new Padding(12, 8, 12, 8);

            lblStatus.Dock = DockStyle.Fill;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            lblStatus.ForeColor = Color.White;
            pnlStatus.Controls.Add(lblStatus);

            pnlClock.Dock = DockStyle.Right;
            pnlClock.Width = 170;
            pnlClock.BackColor = Color.FromArgb(42, 66, 93);
            pnlClock.Padding = new Padding(8);

            lblClock.Dock = DockStyle.Fill;
            lblClock.TextAlign = ContentAlignment.MiddleCenter;
            lblClock.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            pnlClock.Controls.Add(lblClock);

            pnlControls.Dock = DockStyle.Right;
            pnlControls.Width = 545;
            pnlControls.BackColor = Color.FromArgb(31, 48, 69);
            pnlControls.Padding = new Padding(8, 20, 8, 20);

            FlowLayoutPanel buttons = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0),
                BackColor = Color.Transparent
            };

            ConfigureButton(btnPhilo, "Philo", (_, _) => Launch(PhiloUrl));
            ConfigureButton(btnYouTube, "YouTube", (_, _) => Launch(YouTubeUrl));
            ConfigureButton(btnExample, "Example", (_, _) => Launch(ExampleUrl));
            ConfigureButton(btnStop, "Stop", (_, _) => _firefox.Stop());
            ConfigureButton(btnClose, "Close Test", (_, _) => Close());

            buttons.Controls.AddRange(
                new Control[] { btnPhilo, btnYouTube, btnExample, btnStop, btnClose });
            pnlControls.Controls.Add(buttons);

            pnlBottomBar.Controls.Add(pnlStatus);
            pnlBottomBar.Controls.Add(pnlControls);
            pnlBottomBar.Controls.Add(pnlClock);

            Controls.Add(pnlBrowserSurface);
            Controls.Add(pnlBottomBar);
        }

        private static void ConfigureButton(
            Button button,
            string text,
            EventHandler onClick)
        {
            button.Text = text;
            button.Width = 96;
            button.Height = 48;
            button.Margin = new Padding(4, 4, 4, 4);
            button.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            button.UseVisualStyleBackColor = true;
            button.Click += onClick;
        }

        private Rectangle GetBrowserScreenRectangle()
        {
            if (!pnlBrowserSurface.IsHandleCreated)
                return Rectangle.Empty;

            return pnlBrowserSurface.RectangleToScreen(pnlBrowserSurface.ClientRectangle);
        }

        private void Launch(string url)
        {
            try
            {
                lblStatus.Text = $"Launching {url}";
                _firefox.Launch(url);
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Launch failed: {ex.Message}";
                Debug.WriteLine($"Firefox kiosk experiment launch failed: {ex}");
            }
        }

        private void Firefox_StatusChanged(object? sender, string message)
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => lblStatus.Text = message));
                return;
            }

            lblStatus.Text = message;
        }

        private void UpdateClock()
        {
            lblClock.Text = DateTime.Now.ToString("h:mm:ss tt\r\nMMM d, yyyy");
        }

        private void FrmFirefoxKioskExperiment_FormClosing(
            object? sender,
            FormClosingEventArgs e)
        {
            _clockTimer.Stop();
            _firefox.Dispose();
        }
    }
}
