using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InfoDisplayApp
{
    public partial class ctrlPhiloWebView : UserControl
    {
        // Instead of relying on a disruptive full-page reload every few hours,
        // periodically give the active HTML5 video element a very short
        // pause/play pulse. This asks Chromium to re-anchor the media clocks while
        // preserving the current Philo page, channel, cookies, and login state.
        private static readonly TimeSpan SoftResyncInterval = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan HardRecoveryInterval = TimeSpan.FromHours(4);
        private static readonly TimeSpan HiddenResyncThreshold = TimeSpan.FromMinutes(5);

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
        /// Performs a lightweight media-clock resync without navigating away from
        /// the current Philo page. If Philo has an actively playing video, it is
        /// paused for roughly 120 ms and immediately resumed at the same playback
        /// position/rate. This is intentionally much less disruptive than reload.
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

    for (const video of videos) {
        const muted = video.muted;
        const volume = video.volume;
        const playbackRate = video.playbackRate;

        video.pause();
        await new Promise(resolve => setTimeout(resolve, 120));

        video.muted = muted;
        video.volume = volume;
        video.playbackRate = playbackRate || 1.0;

        try {
            await video.play();
        } catch {
            // If autoplay policy rejects the resume, leave Philo itself in
            // control rather than turning a routine resync into an error popup.
        }
    }

    return 'resynced';
})();";

                string result = await wvPhilo.ExecuteScriptAsync(script);

                if (result.Contains("resynced", StringComparison.OrdinalIgnoreCase))
                    Debug.WriteLine("Philo: performed lightweight A/V resync pulse.");
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
        /// Unlike the old implementation, this waits for navigation to actually
        /// finish before allowing another recovery operation to begin.
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
                        Debug.WriteLine("Philo: full recovery reload timed out waiting for navigation.");
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
                    // Switching back from camera/YouTube after a while is a good
                    // opportunity to reset the media clocks before the user starts
                    // watching Philo again.
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
