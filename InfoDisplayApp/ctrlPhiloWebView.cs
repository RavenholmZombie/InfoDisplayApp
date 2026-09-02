using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InfoDisplayApp
{
    public partial class ctrlPhiloWebView : UserControl
    {
        private static readonly TimeSpan PlaybackRecoveryInterval = TimeSpan.FromHours(4);
        private readonly System.Windows.Forms.Timer _playbackRecoveryTimer;
        private bool _muted;
        private bool _recoveryInProgress;

        public ctrlPhiloWebView()
        {
            InitializeComponent();

            _playbackRecoveryTimer = new System.Windows.Forms.Timer
            {
                Interval = (int)PlaybackRecoveryInterval.TotalMilliseconds
            };

            _playbackRecoveryTimer.Tick += PlaybackRecoveryTimer_Tick;
            Disposed += ctrlPhiloWebView_Disposed;

            // The timer only runs while Philo is actually being displayed.
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
        /// Performs a lightweight recovery of Philo's current playback session.
        /// Reloading the current WebView page rebuilds Chromium's media pipeline
        /// without throwing away the WebView2 profile, cookies, or Philo login.
        /// </summary>
        public async Task ResetPhiloPlayerAsync()
        {
            if (_recoveryInProgress || wvPhilo?.CoreWebView2 == null)
                return;

            _recoveryInProgress = true;

            try
            {
                Debug.WriteLine("Philo: refreshing playback pipeline to prevent A/V drift.");

                wvPhilo.CoreWebView2.Reload();

                // NavigationCompleted will re-apply the requested mute state.
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Philo playback recovery failed: {ex}");
            }
            finally
            {
                _recoveryInProgress = false;
            }
        }

        private async void PlaybackRecoveryTimer_Tick(object? sender, EventArgs e)
        {
            _playbackRecoveryTimer.Stop();

            try
            {
                if (Visible)
                    await ResetPhiloPlayerAsync();
            }
            finally
            {
                if (!IsDisposed && Visible)
                    _playbackRecoveryTimer.Start();
            }
        }

        private void ctrlPhiloWebView_VisibleChanged(object? sender, EventArgs e)
        {
            if (Visible)
            {
                _playbackRecoveryTimer.Stop();
                _playbackRecoveryTimer.Start();
            }
            else
            {
                _playbackRecoveryTimer.Stop();
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
            _playbackRecoveryTimer.Stop();
            _playbackRecoveryTimer.Dispose();
        }
    }
}
