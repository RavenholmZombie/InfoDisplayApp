using InfoDisplayApp.Properties;
using InfoDisplayApp.Services;
using System.Diagnostics;

namespace InfoDisplayApp;

public partial class ctrlAppsPanel : UserControl
{
    private readonly AppletManager _appletManager = new();
    private static readonly System.Net.Http.HttpClient IconClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    private string ShutdownLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", $"InfoScreen-SHUTDOWN-{Environment.ProcessId}.log");

    public ctrlAppsPanel()
    {
        InitializeComponent();

        lblBtnClose.Click += dbpBtnClose_Click;
        lblBtnClose.MouseEnter += dbpBtnClose_MouseEnter;
        lblBtnClose.MouseLeave += dbpBtnClose_MouseLeave;
        lblBtnClose.Cursor = Cursors.Hand;

        lblBtnRestart.Click += dbpBtnRestart_Click;
        lblBtnRestart.MouseEnter += dbpBtnRestart_MouseEnter;
        lblBtnRestart.MouseLeave += dbpBtnRestart_MouseLeave;
        lblBtnRestart.Cursor = Cursors.Hand;

        flowLayoutPanel1.AutoScroll = true;
        flowLayoutPanel1.WrapContents = true;
        // Four fixed rows: entries fill top-to-bottom, then start a new column.
        // The drawer is wide enough for the normal app set; scrolling remains
        // available only if the installed collection eventually outgrows it.
        flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
        flowLayoutPanel1.Padding = new Padding(8, 6, 8, 0);
        RebuildApps();
    }

    public void RebuildApps()
    {
        flowLayoutPanel1.SuspendLayout();
        try
        {
            flowLayoutPanel1.Controls.Clear();

            AddSystemApp("Tapo", Resources.tapo_icn, () => Main()?.ShowCameraMode());
            AddSystemApp("Browser", Resources.browser_icn, () => Main()?.ShowBrowserMode());
            AddSystemApp("TEST ALERT", Resources.alert_icn, () => Main()?.TriggerNationalPeriodicTest());
            AddSystemApp("InfoStore", Resources.store_icn, OpenInfoStore);

            foreach (AppletDefinition applet in _appletManager.GetInstalledApplets())
                AddApplet(applet);
        }
        finally
        {
            flowLayoutPanel1.ResumeLayout(true);
        }
    }

    private frmMain? Main() => Application.OpenForms.OfType<frmMain>().FirstOrDefault();

    private void AddSystemApp(string name, Image icon, Action action)
    {
        Panel tile = CreateTile(name, icon, action);
        flowLayoutPanel1.Controls.Add(tile);
    }

    private void AddApplet(AppletDefinition applet)
    {
        PictureBox iconBox;
        Panel tile = CreateTile(applet.Name, SystemIcons.Application.ToBitmap(),
            () => Main()?.ShowApplet(applet), out iconBox);
        flowLayoutPanel1.Controls.Add(tile);

        string? cachedIcon = _appletManager.GetCachedIconPath(applet.Id);
        if (cachedIcon != null && TryLoadIcon(iconBox, cachedIcon))
            return;

        if (!string.IsNullOrWhiteSpace(applet.IconUrl))
            _ = LoadRemoteIconAsync(iconBox, applet.IconUrl);
    }

    private Panel CreateTile(string name, Image icon, Action action) =>
        CreateTile(name, icon, action, out _);

    private Panel CreateTile(string name, Image icon, Action action, out PictureBox iconBox)
    {
        // Keep every drawer entry on the same fixed tile geometry. Some service
        // artwork has very different source aspect ratios/canvas padding, so the
        // PictureBox itself is centered explicitly rather than relying on the
        // artwork dimensions.
        const int tileWidth = 86;
        const int tileHeight = 92;
        const int iconSize = 50;
        const int iconTop = 10;
        const int labelTop = 62;

        Panel tile = new()
        {
            Size = new Size(tileWidth, tileHeight),
            Margin = new Padding(3, 2, 9, 2),
            Cursor = Cursors.Hand
        };

        iconBox = new PictureBox
        {
            Image = icon,
            Location = new Point((tileWidth - iconSize) / 2, iconTop),
            Size = new Size(iconSize, iconSize),
            SizeMode = PictureBoxSizeMode.Zoom,
            Cursor = Cursors.Hand
        };

        Label label = new()
        {
            AutoEllipsis = true,
            Location = new Point(2, labelTop),
            Size = new Size(tileWidth - 4, 28),
            Text = name,
            TextAlign = ContentAlignment.TopCenter,
            Cursor = Cursors.Hand
        };

        void Click(object? sender, EventArgs e) => action();
        tile.Click += Click;
        iconBox.Click += Click;
        label.Click += Click;

        tile.Controls.Add(label);
        tile.Controls.Add(iconBox);
        return tile;
    }

    private static bool TryLoadIcon(PictureBox box, string path)
    {
        try
        {
            using Image image = Image.FromFile(path);
            box.Image = new Bitmap(image);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task LoadRemoteIconAsync(PictureBox box, string url)
    {
        try
        {
            byte[] bytes = await IconClient.GetByteArrayAsync(url);
            using MemoryStream stream = new(bytes);
            using Image image = Image.FromStream(stream);
            if (!box.IsDisposed) box.Image = new Bitmap(image);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to load applet icon {url}: {ex.Message}");
        }
    }

    private void OpenInfoStore()
    {
        Main()?.ShowInfoStore();
    }

    private void LogShutdown(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ShutdownLogPath)!);
            File.AppendAllText(ShutdownLogPath, $"{DateTime.Now:O} ctrlAppsPanel {message}{Environment.NewLine}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to write shutdown diagnostics: {ex}");
        }
    }

    private void ctrlAppsPanel_Load(object sender, EventArgs e) { }

    private void dbpBtnClose_MouseEnter(object sender, EventArgs e) =>
        dbpBtnClose.BackgroundImage = Resources.glass_btn_close_hover;

    private void dbpBtnClose_MouseLeave(object sender, EventArgs e) =>
        dbpBtnClose.BackgroundImage = Resources.glass_btn_close_norm;

    private void dbpBtnRestart_MouseEnter(object sender, EventArgs e) =>
        dbpBtnRestart.BackgroundImage = Resources.glass_btn_restart_hover;

    private void dbpBtnRestart_MouseLeave(object sender, EventArgs e) =>
        dbpBtnRestart.BackgroundImage = Resources.glass_btn_restart_norm;

    private void dbpBtnClose_Click(object sender, EventArgs e)
    {
        if (!AppMessages.AskYesNo("Do you wish to close InfoScreen?")) return;
        LogShutdown("Close confirmed.");
        frmMain? mainForm = Main();
        mainForm?.PrepareForShutdown();
        using frmClosing closing = new();
        closing.setRestarting(false);
        closing.ShowDialog(this);
    }

    private void dbpBtnRestart_Click(object sender, EventArgs e)
    {
        if (!AppMessages.AskYesNo("Do you wish to restart InfoScreen?")) return;
        LogShutdown("Restart confirmed.");
        frmMain? mainForm = Main();
        mainForm?.PrepareForShutdown();
        using frmClosing closing = new();
        closing.setRestarting(true);
        closing.ShowDialog(this);
    }

    // Kept for designer event bindings left in the legacy layout. The dynamic
    // panel clears those legacy tiles immediately after InitializeComponent().
    private void appPnlPhilo_Click(object sender, EventArgs e) { }
    private void appPnlYouTube_Click(object sender, EventArgs e) { }
    private void appPnlTapo_Click(object sender, EventArgs e) { }
    private void appPnlBrowser_Click(object sender, EventArgs e) { }
}
