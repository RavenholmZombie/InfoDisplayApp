using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InfoDisplayApp
{
    public partial class ctrlPhiloWebView : UserControl
    {
        // Philo's long-running live stream can slowly drift in WebView2. A plain
        // pause/play was not always enough to make Chromium rebuild the media
        // timeline, so the lightweight recovery now also performs a tiny seek.
        // This forces the decoder/compositor to re-anchor to the media clock while
        // keeping the user on the same channel and preserving the page/session.
        private static readonly TimeSpan SoftResyncInterval = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan HardRecoveryInterval = TimeSpan.FromHours(2);
        private static readonly TimeSpan HiddenResyncThreshold = TimeSpan.FromMinutes(2);

        private readonly System.Windows.Forms.Timer _softResyncTimer;
        private readonly System.Windows.Forms.Timer _hardRecoveryTimer;
        private bool _muted;
        private bool _recoveryInProgress;
        private DateTime? _hiddenAt;

        public ctrlPhiloWebView()
        {
            InitializeComponent();

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
            Disposed += ctrlPhiloWebView_Disposed;
            VisibleChanged += ctrlPhiloWebView_VisibleChanged;
        }

        public async void SetMuted(bool muted)
        {
            _muted = muted;

            if (wvPhilo?.CoreWebView2 == null)
                return;

            await ApplyMuteStateAsync();
        }

        /// <summary>
        /// Re-anchors Chromium's media pipeline without navigating away from the
        /// active Philo channel. The player is paused briefly and its playhead is
        /// nudged forward by about 50 ms when the stream exposes a seekable range.
        /// Seeking is important here: it makes Chromium flush/reselect a decoded
        /// frame instead of merely restarting the same potentially-drifted clocks.
        /// </summary>
        public async Task SoftResyncPhiloPlayerAsync()
        {
            if (_recoveryInProgress ||
                wvPhilo?.CoreWebView2 == null ||
                !Visible)
            {
                return;
            }

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

        // A pause/play by itself can leave Chromium on the same stale decoder
        // timeline. A microscopic seek makes the media pipeline choose a fresh
        // frame/timestamp pair without producing a noticeable jump in live TV.
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
            // Some DRM/live-player states may temporarily reject a seek. The
            // pause/play pulse below is still safe and useful in that case.
        }

        video.muted = muted;
        video.volume = volume;
        video.playbackRate = playbackRate || 1.0;

        try {
            await video.play();

            // When available, wait until Chromium has actually presented a new
            // video frame before considering the recovery complete.
            if (typeof video.requestVideoFrameCallback === 'function') {
                await Promise.race([
                    new Promise(resolve => video.requestVideoFrameCallback(() => resolve())),
                    new Promise(resolve => setTimeout(resolve, 500))
                ]);
            }

            recovered++;
        } catch {
            // If autoplay policy rejects the resume, leave Philo itself in
            // control rather than turning a maintenance pulse into an error.
        }
    }

    return recovered > 0 ? 'resynced' : 'idle';
})();";

                string result = await wvPhilo.ExecuteScriptAsync(script);

                if (result.Contains("resynced", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine(
                        "Philo: re-anchored WebView2 media pipeline with pause/seek/play pulse.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Philo soft A/V resync failed: {ex.Message}");
            }
            finally
            {
                _recoveryInProgress = false;
            }
        }

        /// <summary>
        /// Performs the stronger fallback recovery by reloading the current page.
        /// This is intentionally infrequent, but now happens before a many-hour
        /// viewing session has enough time to accumulate severe drift.
        /// </summary>
        public async Task ResetPhiloPlayerAsync()
        {
            if (_recoveryInProgress || wvPhilo?.CoreWebView2 == null)
                return;

            _recoveryInProgress = true;

            try
            {
                Debug.WriteLine("Philo: performing full WebView2 playback recovery.");

                TaskCompletionSource<bool> navigationFinished =
                    new(TaskCreationOptions.RunContinuationsAsynchronously);

                void NavigationCompleted(
                    object? sender,
                    Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
                {
                    navigationFinished.TrySetResult(e.IsSuccess);
                }

                wvPhilo.CoreWebView2.NavigationCompleted += NavigationCompleted;

                try
                {
                    wvPhilo.CoreWebView2.Reload();

                    Task finished = await Task.WhenAny(
                        navigationFinished.Task,
                        Task.Delay(TimeSpan.FromSeconds(20)));

                    if (finished != navigationFinished.Task)
                    {
                        Debug.WriteLine(
                            "Philo: full recovery reload timed out waiting for navigation.");
                    }
                }
                finally
                {
                    wvPhilo.CoreWebView2.NavigationCompleted -= NavigationCompleted;
                }

                await ApplyMuteStateAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Philo full playback recovery failed: {ex}");
            }
            finally
            {
                _recoveryInProgress = false;
            }
        }

        private async void SoftResyncTimer_Tick(object? sender, EventArgs e)
        {
            _softResyncTimer.Stop();

            try
            {
                if (Visible)
                    await SoftResyncPhiloPlayerAsync();
            }
            finally
            {
                if (!IsDisposed && Visible)
                    _softResyncTimer.Start();
            }
        }

        private async void HardRecoveryTimer_Tick(object? sender, EventArgs e)
        {
            _hardRecoveryTimer.Stop();

            try
            {
                if (Visible)
                    await ResetPhiloPlayerAsync();
            }
            finally
            {
                if (!IsDisposed && Visible)
                    _hardRecoveryTimer.Start();
            }
        }

        private async void ctrlPhiloWebView_VisibleChanged(object? sender, EventArgs e)
        {
            if (Visible)
            {
                _softResyncTimer.Stop();
                _hardRecoveryTimer.Stop();
                _softResyncTimer.Start();
                _hardRecoveryTimer.Start();

                if (_hiddenAt.HasValue &&
                    DateTime.Now - _hiddenAt.Value >= HiddenResyncThreshold)
                {
                    // Returning from a camera/YouTube view is an ideal time to
                    // refresh the playback clocks before the viewer notices drift.
                    await SoftResyncPhiloPlayerAsync();
                }

                _hiddenAt = null;
            }
            else
            {
                _hiddenAt = DateTime.Now;
                _softResyncTimer.Stop();
                _hardRecoveryTimer.Stop();
            }
        }

        private async Task ApplyMuteStateAsync()
        {
            if (wvPhilo?.CoreWebView2 == null)
                return;

            try
            {
                string muted = _muted ? "true" : "false";

                await wvPhilo.ExecuteScriptAsync(
                    $"document.querySelectorAll('video').forEach(v => v.muted = {muted});");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to update Philo mute state: {ex.Message}");
            }
        }

        private async void wvPhilo_CoreWebView2InitializationCompleted(
            object? sender,
            Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess || wvPhilo.CoreWebView2 == null)
                return;

            wvPhilo.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            await ApplyMuteStateAsync();

            if (Visible)
            {
                _softResyncTimer.Start();
                _hardRecoveryTimer.Start();
            }
        }

        private async void CoreWebView2_NavigationCompleted(
            object? sender,
            Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
                return;

            await ApplyMuteStateAsync();
        }

        private void ctrlPhiloWebView_Disposed(object? sender, EventArgs e)
        {
            _softResyncTimer.Stop();
            _hardRecoveryTimer.Stop();
            _softResyncTimer.Dispose();
            _hardRecoveryTimer.Dispose();
        }
    }
}
