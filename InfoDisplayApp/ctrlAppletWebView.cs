using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Diagnostics;

namespace InfoDisplayApp;

public sealed class ctrlAppletWebView : UserControl
{
    private static readonly TimeSpan SoftResyncInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan HardRecoveryInterval = TimeSpan.FromHours(2);
    private static readonly TimeSpan HiddenResyncThreshold = TimeSpan.FromMinutes(2);

    private readonly WebView2 _webView;
    private readonly System.Windows.Forms.Timer _softResyncTimer;
    private readonly System.Windows.Forms.Timer _hardRecoveryTimer;

    private bool _muted;
    private bool _mediaPlayback;
    private bool _recoveryInProgress;
    private DateTime? _hiddenAt;

    public ctrlAppletWebView()
    {
        // Keep the host background consistent with a normal browser. A black default
        // background bleeds through any page areas whose CSS background is transparent.
        _webView = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.White };
        Controls.Add(_webView);
        _webView.CoreWebView2InitializationCompleted += WebView_Initialized;

        _softResyncTimer = new System.Windows.Forms.Timer
        {
            Interval = (int)SoftResyncInterval.TotalMilliseconds
        };
        _hardRecoveryTimer = new System.Windows.Forms.Timer
        {
            Interval = (int)HardRecoveryInterval.TotalMilliseconds
        };

        _softResyncTimer.Tick += SoftResyncTimer_Tick;
        _hardRecoveryTimer.Tick += HardRecoveryTimer_Tick;
        VisibleChanged += AppletWebView_VisibleChanged;
        Disposed += AppletWebView_Disposed;
    }

    public void ConfigureMediaPlayback(bool enabled)
    {
        _mediaPlayback = enabled;
        _hiddenAt = null;
        StopRecoveryTimers();

        if (_mediaPlayback && Visible && _webView.CoreWebView2 != null)
            StartRecoveryTimers();

        Debug.WriteLine($"Applet WebView media A/V recovery: {(enabled ? "enabled" : "disabled")}.");
    }

    public async Task NavigateAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("Applet URL must be an absolute HTTP or HTTPS URL.", nameof(url));

        await _webView.EnsureCoreWebView2Async();
        _webView.Source = uri;
        _webView.CoreWebView2.IsMuted = _muted;

        StopRecoveryTimers();
        if (_mediaPlayback && Visible)
            StartRecoveryTimers();
    }

    public void SetMuted(bool muted)
    {
        _muted = muted;
        if (_webView.CoreWebView2 != null) _webView.CoreWebView2.IsMuted = muted;
    }

    /// <summary>
    /// Re-anchors active HTML5 video elements to Chromium's media clock using the
    /// same pause/tiny-seek/play pulse that previously protected the Philo control.
    /// This is only enabled for applets declaring capabilities.mediaPlayback=true.
    /// </summary>
    public async Task SoftResyncMediaAsync()
    {
        if (!_mediaPlayback || _recoveryInProgress ||
            _webView.CoreWebView2 == null || !Visible)
            return;

        _recoveryInProgress = true;
        try
        {
            const string script = @"
(async () => {
    const videos = Array.from(document.querySelectorAll('video'))
        .filter(v => !v.paused && !v.ended && v.readyState >= 2);

    if (videos.length === 0)
        return 'idle';

    let recovered = 0;
    for (const video of videos) {
        const muted = video.muted;
        const volume = video.volume;
        const playbackRate = video.playbackRate;
        const originalTime = video.currentTime;

        video.pause();
        await new Promise(resolve => setTimeout(resolve, 180));

        try {
            if (video.seekable && video.seekable.length > 0 && Number.isFinite(originalTime)) {
                const rangeIndex = video.seekable.length - 1;
                const rangeStart = video.seekable.start(rangeIndex);
                const rangeEnd = video.seekable.end(rangeIndex);
                let target = originalTime + 0.05;
                target = Math.max(rangeStart + 0.01, Math.min(target, rangeEnd - 0.01));

                if (Number.isFinite(target) && Math.abs(target - originalTime) >= 0.001) {
                    const seekFinished = new Promise(resolve => {
                        const done = () => resolve();
                        video.addEventListener('seeked', done, { once: true });
                        setTimeout(done, 400);
                    });
                    video.currentTime = target;
                    await seekFinished;
                }
            }
        } catch {
            // DRM/live players can reject seeking. The pause/play pulse remains useful.
        }

        video.muted = muted;
        video.volume = volume;
        video.playbackRate = playbackRate || 1.0;

        try {
            await video.play();
            if (typeof video.requestVideoFrameCallback === 'function') {
                await Promise.race([
                    new Promise(resolve => video.requestVideoFrameCallback(() => resolve())),
                    new Promise(resolve => setTimeout(resolve, 500))
                ]);
            }
            recovered++;
        } catch {
            // Leave autoplay-policy failures to the site's own player.
        }
    }

    return recovered > 0 ? 'resynced' : 'idle';
})();";

            string result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
            if (result.Contains("resynced", StringComparison.OrdinalIgnoreCase))
                Debug.WriteLine("Applet media: WebView2 A/V pipeline re-anchored.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Applet media soft A/V resync failed: {ex.Message}");
        }
        finally
        {
            _recoveryInProgress = false;
        }
    }

    private async Task HardRecoverMediaAsync()
    {
        if (!_mediaPlayback || _recoveryInProgress ||
            _webView.CoreWebView2 == null || !Visible)
            return;

        _recoveryInProgress = true;
        try
        {
            Debug.WriteLine("Applet media: performing scheduled WebView2 reload recovery.");

            TaskCompletionSource<bool> navigationFinished =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            void NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e) =>
                navigationFinished.TrySetResult(e.IsSuccess);

            _webView.CoreWebView2.NavigationCompleted += NavigationCompleted;
            try
            {
                _webView.CoreWebView2.Reload();
                await Task.WhenAny(navigationFinished.Task, Task.Delay(TimeSpan.FromSeconds(20)));
                _webView.CoreWebView2.IsMuted = _muted;
            }
            finally
            {
                _webView.CoreWebView2.NavigationCompleted -= NavigationCompleted;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Applet media hard A/V recovery failed: {ex.Message}");
        }
        finally
        {
            _recoveryInProgress = false;
        }
    }

    private async void SoftResyncTimer_Tick(object? sender, EventArgs e)
    {
        _softResyncTimer.Stop();
        try { await SoftResyncMediaAsync(); }
        finally
        {
            if (!IsDisposed && _mediaPlayback && Visible)
                _softResyncTimer.Start();
        }
    }

    private async void HardRecoveryTimer_Tick(object? sender, EventArgs e)
    {
        _hardRecoveryTimer.Stop();
        try { await HardRecoverMediaAsync(); }
        finally
        {
            if (!IsDisposed && _mediaPlayback && Visible)
                _hardRecoveryTimer.Start();
        }
    }

    private async void AppletWebView_VisibleChanged(object? sender, EventArgs e)
    {
        if (!_mediaPlayback)
        {
            StopRecoveryTimers();
            _hiddenAt = null;
            return;
        }

        if (Visible)
        {
            StartRecoveryTimers();

            if (_hiddenAt.HasValue &&
                DateTime.Now - _hiddenAt.Value >= HiddenResyncThreshold)
            {
                await SoftResyncMediaAsync();
            }

            _hiddenAt = null;
        }
        else
        {
            _hiddenAt = DateTime.Now;
            StopRecoveryTimers();
        }
    }

    private void StartRecoveryTimers()
    {
        _softResyncTimer.Stop();
        _hardRecoveryTimer.Stop();
        _softResyncTimer.Start();
        _hardRecoveryTimer.Start();
    }

    private void StopRecoveryTimers()
    {
        _softResyncTimer.Stop();
        _hardRecoveryTimer.Stop();
    }

    private void WebView_Initialized(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (e.IsSuccess && _webView.CoreWebView2 != null)
        {
            _webView.CoreWebView2.IsMuted = _muted;
            _webView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Light;

            if (_mediaPlayback && Visible)
                StartRecoveryTimers();
        }
    }

    private void AppletWebView_Disposed(object? sender, EventArgs e)
    {
        StopRecoveryTimers();
        _softResyncTimer.Dispose();
        _hardRecoveryTimer.Dispose();
    }
}
