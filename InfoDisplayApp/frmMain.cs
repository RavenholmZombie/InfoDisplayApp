using InfoDisplayApp.Properties;
using InfoDisplayApp.Services;
using NAudio.Wave;
using System.Diagnostics;
using System.IO;

namespace InfoDisplayApp
{
    public partial class frmMain : Form
    {
        private ctrlAppletWebView? _appletView;
        private frmInfoStore? _infoStore;
        private Panel? _appletLandingPage;
        private ctrlCameras? _cameraView;
        private ctrlAppsPanel? _appsPanel;
        private ctrlTicker? _normalTicker;
        private ctrlEmergencyTicker? _emergencyTicker;
        private frmBrowser? _browserForm;
        private frmApps? _appsForm;
        private WaveOutEvent? _startupAudioOutput;
        private WaveFileReader? _startupAudioReader;
        private MemoryStream? _startupAudioStream;
        private DisplayDiagnosticsMonitor? _displayDiagnosticsMonitor;
        private WebBrowser? _offlineView;
        private readonly ConnectivityService _connectivity = new();
        // Start in a connectivity-unknown state. Network-backed UI is held until
        // the first probe completes so WebView2 cannot flash its own offline page.
        private bool _offlineMode = true;

        private readonly Random _random = new Random();
        private readonly System.Windows.Forms.Timer _colorTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer _alertPollTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer _appletUpdateTimer = new System.Windows.Forms.Timer();
        private readonly NwsAlertService _nwsAlertService = new();
        private readonly AppletManager _appletManager = new();
        private readonly AppletRepositoryService _appletRepository = new();
        private readonly Queue<NwsAlertMessage> _pendingAlerts = new();
        private readonly HashSet<string> _seenAlertIds = new(StringComparer.OrdinalIgnoreCase);

        private bool _startupSoundPlayed;
        private bool _alertPollInProgress;
        private bool _emergencyAlertActive;
        private bool _appletUpdateCheckInProgress;
        private string? _lastPromptedUpdateSignature;
        private string? _currentAlertId;

        private const string LastAppStateFileName = "last-app.json";
        private string LastAppStatePath =>
            Path.Combine(new AppletManager().AppletsDirectory, LastAppStateFileName);

        public string tickerMode = "normal";

        private string ShutdownLogPath =>
            Path.Combine(AppContext.BaseDirectory, "logs",
                $"InfoScreen-SHUTDOWN-{Environment.ProcessId}.log");

        private void LogShutdown(string message)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ShutdownLogPath)!);
                File.AppendAllText(ShutdownLogPath,
                    $"{DateTime.Now:O} frmMain {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to write shutdown diagnostics: {ex}");
            }
        }

        private Color _startColor;
        private Color _targetColor;

        private int _fadeStep = 0;
        private const int FadeSteps = 200;

        public frmMain()
        {
            InitializeComponent();

            // Suppress network-derived message windows during the initial
            // connectivity probe. The result will explicitly enable/disable this.
            AppMessages.OfflineMode = true;

            _startColor = RandomColor();
            _targetColor = RandomColor();

            BackColor = _startColor;

            DoubleBuffered = true;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true
            );

            UpdateStyles();

            _colorTimer.Interval = 30;
            _colorTimer.Tick += ColorTimer_Tick;
            _colorTimer.Start();

            _alertPollTimer.Interval = 60_000;
            _alertPollTimer.Tick += AlertPollTimer_Tick;

            // Check periodically while InfoScreen is running. The first check is
            // kicked off once the visible main UI is ready. Keep automatic polling
            // conservative so the unauthenticated GitHub API rate limit is respected.
            _appletUpdateTimer.Interval = (int)TimeSpan.FromMinutes(15).TotalMilliseconds;
            _appletUpdateTimer.Tick += AppletUpdateTimer_Tick;

            _connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
            _connectivity.ProbeStatusChanged += Connectivity_ProbeStatusChanged;

            pboxAppsIcon.MouseEnter += pnlBtnApps_MouseEnter;
            pboxAppsIcon.MouseLeave += pnlBtnApps_MouseLeave;
            pboxAppsIcon.Click += pnlBtnApps_Click;
        }

        internal void NotifyStartupVisible()
        {
            if (_startupSoundPlayed || !Visible || Opacity <= 0)
                return;

            _startupSoundPlayed = true;
            AudioPathology.BeginSession();
            AudioEndpointDiagnostics.Start();
            _displayDiagnosticsMonitor ??= new DisplayDiagnosticsMonitor();
            _displayDiagnosticsMonitor.Start();
            _ = PlayStartupSoundAsync();

            _alertPollTimer.Start();
            _appletUpdateTimer.Start();

            // Initial connectivity is resolved by StartupApplicationContext before
            // frmMain is revealed. Now that AppMessages has a visible owner and the
            // dashboard is interactive, perform the first repository update check.
            // The 15-minute timer handles subsequent checks.
            if (!_offlineMode)
                _ = CheckForAppletUpdatesAsync();
        }

        private async Task PlayStartupSoundAsync()
        {
            try
            {
                if (IsDisposed || Disposing)
                {
                    Debug.WriteLine("STARTUP AUDIO: cancelled because frmMain is closing or disposed.");
                    return;
                }

                Debug.WriteLine("STARTUP AUDIO: NAudio waveOut playback beginning immediately.");

                byte[] wavBytes;
                using (Stream resourceStream = Resources.sfx_startup)
                using (MemoryStream copy = new())
                {
                    resourceStream.Position = 0;
                    resourceStream.CopyTo(copy);
                    wavBytes = copy.ToArray();
                }

                AudioPathology.InspectWave("STARTUP", wavBytes);

                _startupAudioStream = new MemoryStream(wavBytes, writable: false);
                _startupAudioReader = new WaveFileReader(_startupAudioStream);
                _startupAudioOutput = new WaveOutEvent();
                _startupAudioOutput.Init(_startupAudioReader);

                Stopwatch playbackClock = Stopwatch.StartNew();
                TaskCompletionSource completion =
                    new(TaskCreationOptions.RunContinuationsAsynchronously);

                void PlaybackStopped(object? sender, StoppedEventArgs e)
                {
                    AudioPathology.LogPlaybackStopped(
                        "STARTUP",
                        _startupAudioReader,
                        _startupAudioOutput,
                        playbackClock,
                        e.Exception);

                    if (e.Exception != null)
                        completion.TrySetException(e.Exception);
                    else
                        completion.TrySetResult();
                }

                _startupAudioOutput.PlaybackStopped += PlaybackStopped;
                _startupAudioOutput.Play();
                AudioPathology.LogPlaybackStarted(
                    "STARTUP",
                    _startupAudioReader,
                    _startupAudioOutput,
                    playbackClock);
                Debug.WriteLine("STARTUP AUDIO: NAudio waveOut Play() started.");

                await completion.Task;
                Debug.WriteLine("STARTUP AUDIO: NAudio WASAPI playback completed.");

                _startupAudioOutput.PlaybackStopped -= PlaybackStopped;

             }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to play startup sound with NAudio: {ex}");
            }
            finally
            {
                DisposeStartupAudio();
            }
        }

        private void DisposeStartupAudio()
        {
            try { _startupAudioOutput?.Stop(); }
            catch { }

            _startupAudioOutput?.Dispose();
            _startupAudioReader?.Dispose();
            _startupAudioStream?.Dispose();

            _startupAudioOutput = null;
            _startupAudioReader = null;
            _startupAudioStream = null;

            Debug.WriteLine("STARTUP AUDIO: NAudio resources disposed.");
        }

        private Color RandomColor()
        {
            return Color.FromArgb(
                _random.Next(50, 220),
                _random.Next(50, 220),
                _random.Next(50, 220)
            );
        }

        private void ColorTimer_Tick(object? sender, EventArgs e)
        {
            _fadeStep++;

            double progress = (double)_fadeStep / FadeSteps;

            int r = (int)(_startColor.R +
                (_targetColor.R - _startColor.R) * progress);

            int g = (int)(_startColor.G +
                (_targetColor.G - _startColor.G) * progress);

            int b = (int)(_startColor.B +
                (_targetColor.B - _startColor.B) * progress);

            BackColor = Color.FromArgb(r, g, b);

            if (_fadeStep >= FadeSteps)
            {
                _startColor = _targetColor;
                _targetColor = RandomColor();
                _fadeStep = 0;
            }
        }

        public void ToggleTickerMode(string mode)
        {
            if (mode.Equals("normal", StringComparison.OrdinalIgnoreCase))
            {
                EndEmergencyAlertSequence();
                return;
            }

            _ = PollAlertsAsync(replayActiveAlert: true);
        }

        public void TriggerNationalPeriodicTest()
        {
            if (IsDisposed || _emergencyAlertActive)
                return;

            DateTimeOffset now = DateTimeOffset.Now;

            const string displayText =
                "This is a test of the National Emergency Alert System. " +
                "This system was developed by broadcast and cable operators in voluntary cooperation with the Federal Emergency Management Agency, the Federal Communications Commission, and local authorities to keep you informed in the event of an emergency. " +
                "If this had been an actual emergency, an official message would have followed the tone-alert you heard at the start of this message. No action is required.";

            const string speechText =
                "This is a test of the National Emergency Alert System. " +
                "This system was developed by broadcast and cable operators in voluntary cooperation with the Federal Emergency Management Agency, the Federal Communications Commission, and local authorities to keep you informed in the event of an emergency. " +
                "If this had been an actual emergency, an official message would have followed the tone-alert you heard at the start of this message. No action is required.";

            NwsAlertMessage testAlert = new(
                $"InfoDisplay-NPT-{now:yyyyMMddHHmmssfff}",
                "National Periodic Test",
                displayText,
                speechText,
                "Minor",
                "Expected",
                "InfoDisplay Test Generator",
                "Princeton-Calais, ME",
                now,
                now.AddMinutes(15),
                1);

            _pendingAlerts.Enqueue(testAlert);
            BeginNextEmergencyAlert();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            _appletView = new ctrlAppletWebView
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            pnlTV.Controls.Add(_appletView);

            _cameraView = new ctrlCameras
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            pnlTV.Controls.Add(_cameraView);

            // Do not restore a WebView-backed app until the first connectivity
            // probe completes. Keep our local page in front so WebView2 never
            // flashes its built-in dinosaur/offline error page.
            _offlineView = CreateOfflineView();
            pnlTV.Controls.Add(_offlineView);
            _offlineView.Visible = true;
            _offlineView.BringToFront();

            ctrlTimeDate ctrlTimeDate = new ctrlTimeDate
            {
                Dock = DockStyle.Fill
            };
            pnlDateTime.Controls.Add(ctrlTimeDate);

            _normalTicker = new ctrlTicker
            {
                Dock = DockStyle.Fill
            };
            _normalTicker.SetOfflineMode(true);
            pnlTicker.Controls.Add(_normalTicker);

            ctrlWeather ctrlWeather = new ctrlWeather
            {
                Dock = DockStyle.Fill
            };
            pnlWeather.Controls.Add(ctrlWeather);

            // pnlApps remains in the designer as a positioning/sizing anchor only.
            // The actual Apps UI lives in its own top-level borderless window so
            // it does not overlap WebView2/LibVLC native child HWNDs.
            pnlApps.Visible = false;

            _appsPanel = new ctrlAppsPanel
            {
                Dock = DockStyle.Fill
            };
            _appsPanel.SetOfflineMode(true);

            _appsForm = new frmApps(_appsPanel);
            PositionAppsForm();

            UpdateModeButtons(true);
        }

        private async Task InitializeConnectivityAsync()
        {
            await _connectivity.StartAsync();

            if (IsDisposed || Disposing)
                return;

            if (InvokeRequired)
            {
                BeginInvoke((Action)(() =>
                    Connectivity_ConnectivityChanged(_connectivity, _connectivity.IsOnline)));
                return;
            }

            Connectivity_ConnectivityChanged(_connectivity, _connectivity.IsOnline);
        }

        private void Connectivity_ProbeStatusChanged(object? sender, ConnectivityProbeEventArgs e)
        {
            if (IsDisposed || Disposing)
                return;

            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => Connectivity_ProbeStatusChanged(sender, e)));
                return;
            }

            // While online these 30-second checks stay invisible. Once Offline
            // Mode is active, surface the retry/result directly on the local page.
            if (!_offlineMode)
                return;

            string text = e.State switch
            {
                ConnectivityProbeState.Checking => "Checking for Internet connection...",
                ConnectivityProbeState.Failed => "Failed. Internet is unreachable.",
                ConnectivityProbeState.Succeeded => "Connectivity restored. Switching out of offline mode...",
                _ => ""
            };

            SetOfflineStatus(text);
        }

        private void SetOfflineStatus(string text)
        {
            if (_offlineView?.Document == null)
                return;

            try
            {
                HtmlElement? status = _offlineView.Document.GetElementById("connectionStatus");
                if (status != null)
                    status.InnerText = text;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OFFLINE MODE: unable to update connection status: {ex.Message}");
            }
        }

        private void Connectivity_ConnectivityChanged(object? sender, bool online)
        {
            if (IsDisposed || Disposing)
                return;

            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => Connectivity_ConnectivityChanged(sender, online)));
                return;
            }

            if (online)
                _ = ExitOfflineModeAsync();
            else
                EnterOfflineMode();
        }

        private void EnterOfflineMode()
        {
            bool alreadyOffline = _offlineMode;
            _offlineMode = true;
            AppMessages.OfflineMode = true;
            _normalTicker?.SetOfflineMode(true);
            _appsPanel?.SetOfflineMode(true);

            if (alreadyOffline && _offlineView?.Visible == true)
                return;

            Debug.WriteLine("OFFLINE MODE: entered.");

            _appsForm?.Hide();
            _infoStore?.Hide();

            if (_appletView != null)
            {
                _appletView.SetMuted(true);
                _appletView.Visible = false;
            }

            if (_cameraView != null)
            {
                _cameraView.SetMuted(true);
                _cameraView.StopCamera();
                _cameraView.Visible = false;
            }

            if (_appletLandingPage != null)
                _appletLandingPage.Visible = false;

            _offlineView ??= CreateOfflineView();
            if (!pnlTV.Controls.Contains(_offlineView))
                pnlTV.Controls.Add(_offlineView);

            _offlineView.Visible = true;
            _offlineView.BringToFront();
        }

        private async Task ExitOfflineModeAsync()
        {
            if (!_offlineMode)
                return;

            SetOfflineStatus("Connectivity restored. Switching out of offline mode...");
            Debug.WriteLine("OFFLINE MODE: Internet connection restored; holding recovery message.");

            // Give the user a moment to see that recovery succeeded instead of
            // instantly replacing the page at the exact moment the probe returns.
            await Task.Delay(TimeSpan.FromSeconds(3));

            if (IsDisposed || Disposing || !_connectivity.IsOnline)
                return;

            ApplyOnlineMode(restoreContent: true);

            _ = PollAlertsAsync();
            _ = CheckForAppletUpdatesAsync();
        }

        private void ApplyOnlineMode(bool restoreContent)
        {
            // Keep every Offline Mode participant in lockstep. Previously startup
            // cleared frmMain/ticker state but forgot the Apps drawer, producing a
            // half-online state (web app restored while the drawer stayed offline).
            _offlineMode = false;
            AppMessages.OfflineMode = false;
            _normalTicker?.SetOfflineMode(false);
            _appsPanel?.SetOfflineMode(false);

            if (_offlineView != null)
                _offlineView.Visible = false;

            if (restoreContent && !TryRestoreLastContent())
                ShowAppletLandingPage();

            Debug.WriteLine("CONNECTIVITY: all components switched to online state.");
        }

        private WebBrowser CreateOfflineView()
        {
            WebBrowser browser = new()
            {
                Dock = DockStyle.Fill,
                ScriptErrorsSuppressed = true,
                IsWebBrowserContextMenuEnabled = false,
                WebBrowserShortcutsEnabled = false,
                AllowNavigation = false,
                BackColor = Color.FromArgb(18, 18, 18)
            };

            using MemoryStream iconStream = new();
            Resources.icn_offline.Save(iconStream, System.Drawing.Imaging.ImageFormat.Png);
            string offlineIcon = Convert.ToBase64String(iconStream.ToArray());

            browser.DocumentText = """
<!doctype html>
<html>
<head>
<meta http-equiv="X-UA-Compatible" content="IE=edge" />
<style>
html,body{height:100%;margin:0;background:#121212;color:#fff;font-family:'Segoe UI',Arial,sans-serif}
.wrap{height:100%;display:flex;align-items:center;justify-content:center;padding:48px;box-sizing:border-box}
.card{max-width:900px;width:100%;background:#202020;border-radius:18px;padding:48px;box-sizing:border-box;box-shadow:0 10px 35px rgba(0,0,0,.35)}
.offline-icon{display:block;width:92px;height:92px;object-fit:contain;margin:0 auto 26px}
h1{font-size:42px;margin:0 0 18px}p{font-size:23px;line-height:1.5;color:#ddd}
h2{font-size:25px;margin:32px 0 12px}li{font-size:20px;line-height:1.65;color:#ddd}
.status{margin-top:32px;padding:18px 22px;background:#2b2b2b;border-radius:10px;font-size:20px;color:#bbb}
</style>
</head>
<body><div class="wrap"><div class="card">
<img class="offline-icon" src="data:image/png;base64,__OFFLINE_ICON__" alt="" />
<h1>InfoScreen requires an Internet connection</h1>
<p>InfoScreen is currently unable to reach the Internet. Local services may continue to operate, but online applets and information services are unavailable.</p>
<h2>Things to try</h2>
<ul>
<li>Make sure your Ethernet cable or Wi-Fi connection is connected.</li>
<li>Check that your router or modem has Internet access.</li>
<li>Try restarting your router or modem.</li>
<li>If other devices are also offline, contact your Internet service provider.</li>
</ul>
<div class="status"><div id="connectionStatus">Checking for Internet connection...</div><div style="margin-top:8px;font-size:16px;color:#999">InfoScreen checks the connection automatically every 30 seconds.</div></div>
</div></div></body></html>
""".Replace("__OFFLINE_ICON__", offlineIcon);
            return browser;
        }

        private async void AlertPollTimer_Tick(object? sender, EventArgs e)
        {
            if (_offlineMode) return;
            await PollAlertsAsync();
        }

        private async void AppletUpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (_offlineMode) return;
            await CheckForAppletUpdatesAsync();
        }

        private async Task CheckForAppletUpdatesAsync()
        {
            if (_offlineMode || _appletUpdateCheckInProgress || IsDisposed || Disposing)
                return;

            _appletUpdateCheckInProgress = true;
            try
            {
                Debug.WriteLine("APPLET UPDATE: checking installed applets against repository.");
                IReadOnlyList<AppletUpdate> updates =
                    await _appletRepository.GetUpdatesAsync(_appletManager);
                Debug.WriteLine($"APPLET UPDATE: {updates.Count} update(s) found.");

                if (updates.Count == 0)
                {
                    _lastPromptedUpdateSignature = null;
                    return;
                }

                string signature = string.Join("|", updates.Select(u =>
                    $"{u.Available.Id}:{u.Installed.Version}>{u.Available.Version}"));

                // Do not nag every 30 minutes for the exact same set of versions.
                if (signature == _lastPromptedUpdateSignature)
                    return;

                _lastPromptedUpdateSignature = signature;

                string summary = updates.Count == 1
                    ? $"{updates[0].Available.Name} can be updated from version {updates[0].Installed.Version} to {updates[0].Available.Version}."
                    : $"{updates.Count} installed applets have updates available.";

                if (AppMessages.AskYesNo(
                    $"{summary}\r\n\r\nWould you like to open the Applet Updates Center now?",
                    "Yes",
                    "No"))
                {
                    ShowInfoStoreUpdates();
                }
            }
            catch (Exception ex)
            {
                // A background repository outage should not interrupt TV viewing,
                // but leave enough diagnostics to make update failures visible.
                Debug.WriteLine($"APPLET UPDATE: background check failed: {ex}");
                LogAppletUpdateCheckFailure(ex);
            }
            finally
            {
                _appletUpdateCheckInProgress = false;
            }
        }

        private static void LogAppletUpdateCheckFailure(Exception ex)
        {
            try
            {
                string directory = Path.Combine(AppContext.BaseDirectory, "logs");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, $"InfoScreen-APPLET-UPDATE-CHECK-{DateTime.Now:yyyy-MM-dd}.log");
                File.AppendAllText(path,
                    $"{DateTime.Now:O} Background update check failed.{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
            }
            catch
            {
                // Update checking must never interrupt normal InfoScreen operation.
            }
        }

        private async Task PollAlertsAsync(bool replayActiveAlert = false)
        {
            if (_offlineMode || _alertPollInProgress || IsDisposed)
                return;

            _alertPollInProgress = true;

            try
            {
                IReadOnlyList<NwsAlertMessage> alerts =
                    await _nwsAlertService.GetActiveAlertsAsync();

                if (replayActiveAlert && alerts.Count > 0)
                {
                    NwsAlertMessage alert = alerts[0];
                    bool alreadyCurrent =
                        _currentAlertId?.Equals(alert.Id, StringComparison.OrdinalIgnoreCase) == true;
                    bool alreadyQueued = _pendingAlerts.Any(item =>
                        item.Id.Equals(alert.Id, StringComparison.OrdinalIgnoreCase));

                    if (!alreadyCurrent && !alreadyQueued)
                        _pendingAlerts.Enqueue(alert);
                }
                else
                {
                    foreach (NwsAlertMessage alert in alerts)
                    {
                        if (_seenAlertIds.Add(alert.Id))
                            _pendingAlerts.Enqueue(alert);
                    }
                }

                if (!_emergencyAlertActive && _pendingAlerts.Count > 0)
                    BeginNextEmergencyAlert();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to update NWS emergency alerts: {ex}");
            }
            finally
            {
                _alertPollInProgress = false;
            }
        }

        private void BeginNextEmergencyAlert()
        {
            if (_emergencyAlertActive || _pendingAlerts.Count == 0 || IsDisposed)
                return;

            NwsAlertMessage alert = _pendingAlerts.Dequeue();
            _currentAlertId = alert.Id;
            _emergencyAlertActive = true;
            tickerMode = "EAS";

            SetApplicationAudioMuted(true);

            if (_normalTicker != null)
                _normalTicker.Visible = false;

            if (_emergencyTicker != null)
            {
                _emergencyTicker.AlertFinished -= EmergencyTicker_AlertFinished;
                pnlTicker.Controls.Remove(_emergencyTicker);
                _emergencyTicker.Dispose();
            }

            _emergencyTicker = new ctrlEmergencyTicker
            {
                Dock = DockStyle.Fill
            };
            _emergencyTicker.AlertFinished += EmergencyTicker_AlertFinished;

            pnlTicker.Controls.Add(_emergencyTicker);
            _emergencyTicker.BringToFront();
            _emergencyTicker.StartAlert(alert);

            Debug.WriteLine(
                $"EAS ticker started: {alert.EventName} ({alert.Severity}/{alert.Urgency}) " +
                $"matched near {alert.MatchedPoint}.");
        }

        private void EmergencyTicker_AlertFinished(object? sender, EventArgs e)
        {
            if (_emergencyTicker != null)
            {
                _emergencyTicker.AlertFinished -= EmergencyTicker_AlertFinished;
                pnlTicker.Controls.Remove(_emergencyTicker);
                _emergencyTicker.Dispose();
                _emergencyTicker = null;
            }

            _emergencyAlertActive = false;
            _currentAlertId = null;

            if (_pendingAlerts.Count > 0)
            {
                BeginNextEmergencyAlert();
                return;
            }

            RestoreNormalTickerAndAudio();
        }

        private void EndEmergencyAlertSequence()
        {
            _pendingAlerts.Clear();

            if (_emergencyTicker != null)
            {
                _emergencyTicker.AlertFinished -= EmergencyTicker_AlertFinished;
                pnlTicker.Controls.Remove(_emergencyTicker);
                _emergencyTicker.Dispose();
                _emergencyTicker = null;
            }

            _emergencyAlertActive = false;
            _currentAlertId = null;
            RestoreNormalTickerAndAudio();
        }

        private void RestoreNormalTickerAndAudio()
        {
            tickerMode = "normal";

            if (_normalTicker != null)
            {
                _normalTicker.Visible = true;
                _normalTicker.BringToFront();
            }

            SetApplicationAudioMuted(false);
        }

        private void SetApplicationAudioMuted(bool muted)
        {
            if (muted)
            {
                _appletView?.SetMuted(true);
                _cameraView?.SetMuted(true);
                _browserForm?.SetMuted(true);
                return;
            }

            if (_appletView != null)
                _appletView.SetMuted(!_appletView.Visible);

            if (_cameraView != null)
                _cameraView.SetMuted(!_cameraView.Visible);

            _browserForm?.SetMuted(false);
        }

        public async void ShowApplet(AppletDefinition applet)
        {
            if (_appletView == null || _cameraView == null)
                return;

            try
            {
                _infoStore?.Hide();
                if (_appletLandingPage != null) _appletLandingPage.Visible = false;
                _cameraView.SetMuted(true);
                _cameraView.StopCamera();
                _cameraView.Visible = false;

                _appletView.Visible = true;
                _appletView.BringToFront();
                _appletView.SetMuted(_emergencyAlertActive);
                _appletView.ConfigureMediaPlayback(applet.Capabilities?.MediaPlayback == true);
                await _appletView.NavigateAsync(applet.Url);
                SaveLastApp("applet", applet.Id);

                _appsForm?.Hide();
                UpdateModeButtons(true);
            }
            catch (Exception ex)
            {
                AppMessages.Error($"Unable to open {applet.Name}: {ex.Message}");
            }
        }

        public void ShowInfoStore()
        {
            ShowInfoStoreCore(openUpdatesCenter: false);
        }

        private void ShowInfoStoreUpdates()
        {
            ShowInfoStoreCore(openUpdatesCenter: true);
        }

        private void ShowInfoStoreCore(bool openUpdatesCenter)
        {
            if (_appletView == null || _cameraView == null)
                return;

            _appsForm?.Hide();
            if (_appletLandingPage != null) _appletLandingPage.Visible = false;
            _cameraView.SetMuted(true);
            _cameraView.StopCamera();
            _cameraView.Visible = false;
            _appletView.SetMuted(true);
            _appletView.Visible = false;

            if (_infoStore == null || _infoStore.IsDisposed)
            {
                _infoStore = new frmInfoStore
                {
                    Dock = DockStyle.Fill,
                    TopLevel = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Visible = false
                };
                _infoStore.AppletsChanged += (_, _) => _appsPanel?.RebuildApps();
                _infoStore.CloseRequested += (_, _) => CloseInfoStore();
                pnlTV.Controls.Add(_infoStore);
            }

            _infoStore.Show();
            _infoStore.BringToFront();

            if (openUpdatesCenter)
                _infoStore.ShowUpdatesCenter();

            UpdateModeButtons(true);
        }

        public void ShowBrowserMode()
        {
            if (_appletView == null || _cameraView == null)
                return;

            _infoStore?.Hide();
            if (_appletLandingPage != null) _appletLandingPage.Visible = false;
            _cameraView.SetMuted(true);
            _cameraView.StopCamera();
            _cameraView.Visible = false;
            _appletView.SetMuted(true);
            _appletView.Visible = false;
            _appsForm?.Hide();

            _browserForm = new frmBrowser();
            _browserForm.SetMuted(_emergencyAlertActive);

            try
            {
                _browserForm.ShowDialog(this);
            }
            finally
            {
                _browserForm.Dispose();
                _browserForm = null;
            }

            // Browser is transient just like InfoStore. Closing it should reveal
            // the last real TV-panel app rather than the blank pnlTV underneath.
            if (!TryRestoreLastContent())
                ShowAppletLandingPage();

            UpdateModeButtons(true);
        }

        public void ShowCameraMode()
        {
            if (_appletView == null || _cameraView == null)
                return;

            _infoStore?.Hide();
            if (_appletLandingPage != null) _appletLandingPage.Visible = false;
            _appletView.Visible = false;
            _appletView.SetMuted(true);

            _cameraView.Visible = true;
            _cameraView.BringToFront();

            _cameraView.SetMuted(_emergencyAlertActive);
            _cameraView.StartCamera();
            SaveLastApp("tapo");
            _appsForm?.Hide();

            UpdateModeButtons(false);
        }

        private void RestoreLastApp()
        {
            if (TryRestoreLastContent())
                return;

            ShowAppletLandingPage();
        }

        private void CloseInfoStore()
        {
            _infoStore?.Hide();

            if (TryRestoreLastContent())
                return;

            ShowAppletLandingPage();
        }

        private bool TryRestoreLastContent()
        {
            try
            {
                if (!File.Exists(LastAppStatePath))
                    return false;

                using System.Text.Json.JsonDocument doc =
                    System.Text.Json.JsonDocument.Parse(File.ReadAllText(LastAppStatePath));

                string type = doc.RootElement.TryGetProperty("type", out var typeElement)
                    ? typeElement.GetString() ?? ""
                    : "";
                string id = doc.RootElement.TryGetProperty("id", out var idElement)
                    ? idElement.GetString() ?? ""
                    : "";

                if (type.Equals("tapo", StringComparison.OrdinalIgnoreCase))
                {
                    ShowCameraMode();
                    return true;
                }

                if (type.Equals("applet", StringComparison.OrdinalIgnoreCase))
                {
                    AppletDefinition? applet = new AppletManager()
                        .GetInstalledApplets()
                        .FirstOrDefault(a => a.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

                    if (applet != null)
                    {
                        ShowApplet(applet);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to restore last app: {ex}");
            }

            return false;
        }

        private void ShowAppletLandingPage()
        {
            _infoStore?.Hide();

            if (_appletView != null)
            {
                _appletView.SetMuted(true);
                _appletView.Visible = false;
            }

            if (_cameraView != null)
            {
                _cameraView.SetMuted(true);
                _cameraView.StopCamera();
                _cameraView.Visible = false;
            }

            if (_appletLandingPage == null || _appletLandingPage.IsDisposed)
            {
                _appletLandingPage = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.FromArgb(18, 18, 18)
                };

                Label title = new()
                {
                    AutoSize = false,
                    Dock = DockStyle.Fill,
                    Text = "Open the Apps Drawer and choose an Applet",
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 26F, FontStyle.Regular, GraphicsUnit.Point)
                };

                _appletLandingPage.Controls.Add(title);
                pnlTV.Controls.Add(_appletLandingPage);
            }

            _appletLandingPage.Visible = true;
            _appletLandingPage.BringToFront();
            _appsForm?.Hide();
            UpdateModeButtons(true);
        }

        private void SaveLastApp(string type, string? id = null)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LastAppStatePath)!);
                string json = System.Text.Json.JsonSerializer.Serialize(
                    new { type, id },
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(LastAppStatePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to save last app: {ex}");
            }
        }

         /// <summary>
        /// Stops and disposes all media hosted by the TV panel before the closing
        /// screen appears. This is used for both exit and restart so no stream
        /// audio continues underneath frmClosing.
        /// </summary>
        public void PrepareForShutdown()
        {
            LogShutdown("PrepareForShutdown entered.");
            _alertPollTimer.Stop();
            _appletUpdateTimer.Stop();
            LogShutdown("Alert and applet update timers stopped.");

            // Mute first so shutdown is silent even if a player takes a moment
            // to release its underlying media session.
            LogShutdown("Muting application audio.");
            SetApplicationAudioMuted(true);
            LogShutdown("Application audio muted.");

            try
            {
                LogShutdown("Stopping camera.");
                _cameraView?.StopCamera();
                LogShutdown("Camera stop returned.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to stop camera during shutdown: {ex}");
            }

            if (_browserForm != null && !_browserForm.IsDisposed)
            {
                try { _browserForm.SetMuted(true); }
                catch (Exception ex) { Debug.WriteLine($"Unable to mute browser during shutdown: {ex}"); }
            }

            // Dispose only the media controls. pnlTV also contains pnlApps,
            // which owns the ctrlAppsPanel currently executing the Close/Restart
            // click handler. Disposing every pnlTV child here tears down the caller
            // mid-event and can freeze shutdown with ObjectDisposedException.
            LogShutdown("Disposing applet WebView control.");
            DisposeTvMediaControl(ref _appletView);
            LogShutdown("Applet WebView control disposed.");

            LogShutdown("Disposing camera media control.");
            DisposeTvMediaControl(ref _cameraView);
            LogShutdown("Camera media control disposed.");

             LogShutdown("PrepareForShutdown completed.");
        }

        private void DisposeTvMediaControl<T>(ref T? control)
            where T : Control
        {
            if (control == null)
                return;

            try
            {
                pnlTV.Controls.Remove(control);
                control.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Unable to dispose {typeof(T).Name} during shutdown: {ex}");
            }
            finally
            {
                control = null;
            }
        }

        private void UpdateModeButtons(bool isPhiloMode)
        {
        }

        private void pnlWeather_Paint(object sender, PaintEventArgs e)
        {
        }

        private void pnlBtnApps_Click(object sender, EventArgs e)
        {
            if (_appsForm == null || _appsForm.IsDisposed)
                return;

            if (_appsForm.Visible)
            {
                _appsForm.Hide();
                return;
            }

            PositionAppsForm();
            _appsForm.Show(this);
            _appsForm.BringToFront();
        }

        private void PositionAppsForm()
        {
            if (_appsForm == null || _appsForm.IsDisposed || !IsHandleCreated)
                return;

            Point screenLocation = pnlApps.PointToScreen(Point.Empty);
            _appsForm.Bounds = new Rectangle(screenLocation, pnlApps.Size);
        }

        private void pnlBtnApps_MouseEnter(object sender, EventArgs e)
        {
            pnlBtnApps.BackgroundImage = Resources.glass_hov;
            pboxAppsIcon.Image = Resources.controls_icn_hov;
        }

        private void pnlBtnApps_MouseLeave(object sender, EventArgs e)
        {
            pnlBtnApps.BackgroundImage = Resources.glass;
            pboxAppsIcon.Image = Resources.controls_icn;
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            LogShutdown($"frmMain_FormClosing entered; reason={e.CloseReason}.");
            if (_appsForm != null && !_appsForm.IsDisposed)
            {
                _appsForm.Close();
                _appsForm.Dispose();
                _appsForm = null;
                LogShutdown("Apps overlay closed and disposed.");
            }

            _alertPollTimer.Stop();
            _alertPollTimer.Dispose();
            _appletUpdateTimer.Stop();
            _appletUpdateTimer.Dispose();
            _connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;
            _connectivity.ProbeStatusChanged -= Connectivity_ProbeStatusChanged;
            _connectivity.Dispose();
            AppMessages.OfflineMode = false;
            LogShutdown("Alert, applet update, and connectivity timers disposed.");

            _displayDiagnosticsMonitor?.Dispose();
            _displayDiagnosticsMonitor = null;
            LogShutdown("Display diagnostics monitor disposed.");

            DisposeStartupAudio();
            LogShutdown("Startup NAudio player disposed.");

            LogShutdown("Ending emergency alert sequence.");
            EndEmergencyAlertSequence();
            LogShutdown("Emergency alert sequence ended; frmMain_FormClosing completed.");
        }
    }
}
