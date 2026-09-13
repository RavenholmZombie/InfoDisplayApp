using CefSharp;
using CefSharp.WinForms;
using System.Diagnostics;

namespace InfoDisplayApp.Experiments
{
    /// <summary>
    /// Standalone CefSharp test harness that mirrors the experimental browser UI:
    /// a large browser surface above a persistent InfoDisplay status/control bar.
    /// Unlike the external-browser experiments, ChromiumWebBrowser is a real
    /// WinForms control and therefore participates directly in the form layout.
    /// </summary>
    internal sealed class frmCefSharpExperiment : Form
    {
        private readonly Panel pnlBrowserSurface = new();
        private readonly Panel pnlBottomBar = new();
        private readonly Panel pnlStatus = new();
        private readonly Panel pnlControls = new();
        private readonly Panel pnlClock = new();
        private readonly Label lblStatus = new();
        private readonly Label lblClock = new();
        private readonly Button btnPhilo = new();
        private readonly Button btnYouTube = new();
        private readonly Button btnExample = new();
        private readonly Button btnStop = new();
        private readonly Button btnClose = new();
        private readonly System.Windows.Forms.Timer _clockTimer = new();
        private ChromiumWebBrowser? _browser;

        private const string PhiloUrl = "https://www.philo.com/player/mytv";
        private const string YouTubeUrl = "https://www.youtube.com/";
        private const string ExampleUrl = "https://example.com/";

        public frmCefSharpExperiment()
        {
            Text = "InfoDisplay CefSharp Experiment";
            BackColor = Color.Black;
            ForeColor = Color.White;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            TopMost = true;
            ShowIcon = false;
            DoubleBuffered = true;

            BuildLayout();
            InitializeBrowser();

            _clockTimer.Interval = 1000;
            _clockTimer.Tick += (_, _) => UpdateClock();
            _clockTimer.Start();
            UpdateClock();

            Shown += (_, _) =>
            {
                lblStatus.Text =
                    "Ready. CefSharp is embedded directly in the InfoDisplay viewport.";
            };

            FormClosing += FrmCefSharpExperiment_FormClosing;
        }

        private void BuildLayout()
        {
            pnlBrowserSurface.Dock = DockStyle.Fill;
            pnlBrowserSurface.BackColor = Color.Black;
            pnlBrowserSurface.Padding = new Padding(0);

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

            ConfigureButton(btnPhilo, "Philo", (_, _) => Navigate(PhiloUrl));
            ConfigureButton(btnYouTube, "YouTube", (_, _) => Navigate(YouTubeUrl));
            ConfigureButton(btnExample, "Example", (_, _) => Navigate(ExampleUrl));
            ConfigureButton(btnStop, "Stop", (_, _) => Navigate("about:blank"));
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

        private void InitializeBrowser()
        {
            _browser = new ChromiumWebBrowser("about:blank")
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };

            _browser.LoadingStateChanged += Browser_LoadingStateChanged;
            _browser.AddressChanged += Browser_AddressChanged;
            _browser.LoadError += Browser_LoadError;

            pnlBrowserSurface.Controls.Add(_browser);
            _browser.BringToFront();
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

        private void Navigate(string url)
        {
            try
            {
                if (_browser is null || _browser.IsDisposed)
                    return;

                SetStatus($"Loading {url}");
                _browser.Load(url);
            }
            catch (Exception ex)
            {
                SetStatus($"Navigation failed: {ex.Message}");
                Debug.WriteLine($"CefSharp experiment navigation failed: {ex}");
            }
        }

        private void Browser_LoadingStateChanged(
            object? sender,
            LoadingStateChangedEventArgs e)
        {
            SetStatus(e.IsLoading
                ? "CefSharp is loading..."
                : "CefSharp page loaded. Test video, audio, DRM, and long-run A/V sync.");
        }

        private void Browser_AddressChanged(
            object? sender,
            AddressChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Address))
                SetStatus($"CefSharp: {e.Address}");
        }

        private void Browser_LoadError(
            object? sender,
            LoadErrorEventArgs e)
        {
            // Chromium reports aborted navigations while switching pages; don't
            // present those routine transitions as hard failures.
            if (e.ErrorCode == CefErrorCode.Aborted)
                return;

            SetStatus($"CefSharp load error {e.ErrorCode}: {e.ErrorText}");
        }

        private void SetStatus(string message)
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

        private void FrmCefSharpExperiment_FormClosing(
            object? sender,
            FormClosingEventArgs e)
        {
            _clockTimer.Stop();

            if (_browser is not null)
            {
                _browser.LoadingStateChanged -= Browser_LoadingStateChanged;
                _browser.AddressChanged -= Browser_AddressChanged;
                _browser.LoadError -= Browser_LoadError;
                _browser.Dispose();
                _browser = null;
            }
        }
    }
}
