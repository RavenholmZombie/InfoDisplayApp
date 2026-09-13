using InfoDisplayApp.Properties;
using InfoDisplayApp.Services;
using System.Diagnostics;
using System.Drawing.Text;
using System.Media;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Threading;

namespace InfoDisplayApp
{
    public partial class ctrlEmergencyTicker : UserControl
    {
        private readonly System.Threading.Timer _animationTimer;
        private readonly Stopwatch _scrollClock = new();
        private SpeechSynthesizer? _speechSynthesizer;
        private NwsAlertMessage? _alert;

        private int _animationFramePending;
        private bool _animationRunning;
        private bool _timerResolutionRequested;
        private double _lastScrollSeconds;
        private double _scrollX;
        private float _messageWidth;
        private bool _completedOneScroll;
        private bool _speechFinished;
        private bool _finishedRaised;

        private const double ScrollPixelsPerSecond = 190.0;
        private const int AnimationPulseMilliseconds = 8;
        private const int MessageGap = 40;
        private const uint TimerResolutionMilliseconds = 1;

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint TimeBeginPeriod(uint period);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern uint TimeEndPeriod(uint period);

        public event EventHandler? AlertFinished;

        public ctrlEmergencyTicker()
        {
            InitializeComponent();

            lblAlertText.Visible = false;
            panel1.Paint += panel1_Paint;
            panel1.Resize += panel1_Resize;

            _animationTimer = new System.Threading.Timer(
                AnimationTimerCallback,
                null,
                Timeout.Infinite,
                Timeout.Infinite);

            Load += ctrlEmergencyTicker_Load;
            Disposed += ctrlEmergencyTicker_Disposed;
        }

        internal void StartAlert(NwsAlertMessage alert)
        {
            if (IsDisposed)
                return;

            _alert = alert;
            _completedOneScroll = false;
            _speechFinished = false;
            _finishedRaised = false;

            label1.Text =
                $"EMERGENCY ALERT SYSTEM MESSAGE - {alert.EventName.ToUpperInvariant()}";

            using Graphics graphics = panel1.CreateGraphics();
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            _messageWidth = graphics.MeasureString(
                alert.DisplayText,
                lblAlertText.Font,
                int.MaxValue,
                StringFormat.GenericTypographic).Width + 12f;

            ResetScrollPosition();
            StartAnimation();
            panel1.Invalidate();
            panel1.Update();

            _ = BeginAlertAudioAsync();
        }

        private void ctrlEmergencyTicker_Load(object? sender, EventArgs e)
        {
            _timerResolutionRequested =
                TimeBeginPeriod(TimerResolutionMilliseconds) == 0;

            // Alert playback is started explicitly by frmMain once the control
            // has been placed on screen and application audio has been muted.
        }

        private void StartAnimation()
        {
            if (_animationRunning || IsDisposed)
                return;

            _animationRunning = true;
            ResetScrollClock();
            _animationTimer.Change(0, AnimationPulseMilliseconds);
        }

        private void StopAnimation()
        {
            if (!_animationRunning)
                return;

            _animationRunning = false;
            _animationTimer.Change(Timeout.Infinite, Timeout.Infinite);
            Interlocked.Exchange(ref _animationFramePending, 0);
        }

        private void AnimationTimerCallback(object? state)
        {
            if (!_animationRunning ||
                IsDisposed ||
                Disposing ||
                !IsHandleCreated)
            {
                return;
            }

            if (Interlocked.Exchange(ref _animationFramePending, 1) != 0)
                return;

            try
            {
                BeginInvoke(new Action(RenderAnimationFrame));
            }
            catch (ObjectDisposedException)
            {
                Interlocked.Exchange(ref _animationFramePending, 0);
            }
            catch (InvalidOperationException)
            {
                Interlocked.Exchange(ref _animationFramePending, 0);
            }
        }

        private void RenderAnimationFrame()
        {
            try
            {
                if (!_animationRunning ||
                    IsDisposed ||
                    _alert == null ||
                    _finishedRaised)
                {
                    return;
                }

                double now = _scrollClock.Elapsed.TotalSeconds;
                double elapsed = Math.Clamp(
                    now - _lastScrollSeconds,
                    0.0,
                    0.050);

                _lastScrollSeconds = now;
                _scrollX -= ScrollPixelsPerSecond * elapsed;

                panel1.Invalidate();
                panel1.Update();

                if (_scrollX + _messageWidth < -MessageGap)
                {
                    _completedOneScroll = true;

                    if (_speechFinished)
                    {
                        TryFinishAlert();
                    }
                    else
                    {
                        ResetScrollPosition();
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref _animationFramePending, 0);
            }
        }

        private void ResetScrollPosition()
        {
            _scrollX = panel1.ClientSize.Width + MessageGap;
            ResetScrollClock();
        }

        private void ResetScrollClock()
        {
            _scrollClock.Restart();
            _lastScrollSeconds = 0;
        }

        private async Task BeginAlertAudioAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    using Stream stream = Resources.alarm;
                    using SoundPlayer player = new(stream);
                    player.Load();
                    player.PlaySync();
                });

                if (IsDisposed || _alert == null)
                    return;

                if (InvokeRequired)
                    BeginInvoke(new Action(StartSpeech));
                else
                    StartSpeech();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to play EAS attention sound: {ex}");
                MarkSpeechFinished();
            }
        }

        private void StartSpeech()
        {
            if (_alert == null || IsDisposed)
            {
                MarkSpeechFinished();
                return;
            }

            try
            {
                _speechSynthesizer?.Dispose();
                _speechSynthesizer = new SpeechSynthesizer();
                _speechSynthesizer.SetOutputToDefaultAudioDevice();

                SelectPreferredEasVoice(_speechSynthesizer);

                _speechSynthesizer.Rate = 0;
                _speechSynthesizer.Volume = 100;
                _speechSynthesizer.SpeakCompleted += SpeechSynthesizer_SpeakCompleted;
                _speechSynthesizer.SpeakAsync(_alert.SpeechText);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Windows TTS could not read the EAS message: {ex}");
                MarkSpeechFinished();
            }
        }

        private static void SelectPreferredEasVoice(SpeechSynthesizer synthesizer)
        {
            try
            {
                InstalledVoice? maleVoice = synthesizer.GetInstalledVoices()
                    .Where(voice => voice.Enabled)
                    .FirstOrDefault(voice =>
                        voice.VoiceInfo.Gender == VoiceGender.Male &&
                        voice.VoiceInfo.Culture.Name.StartsWith(
                            "en-US",
                            StringComparison.OrdinalIgnoreCase));

                maleVoice ??= synthesizer.GetInstalledVoices()
                    .Where(voice => voice.Enabled)
                    .FirstOrDefault(voice =>
                        voice.VoiceInfo.Gender == VoiceGender.Male &&
                        voice.VoiceInfo.Culture.TwoLetterISOLanguageName.Equals(
                            "en",
                            StringComparison.OrdinalIgnoreCase));

                maleVoice ??= synthesizer.GetInstalledVoices()
                    .Where(voice => voice.Enabled)
                    .FirstOrDefault(voice =>
                        voice.VoiceInfo.Gender == VoiceGender.Male);

                if (maleVoice != null)
                {
                    synthesizer.SelectVoice(maleVoice.VoiceInfo.Name);
                    Debug.WriteLine(
                        $"EAS TTS voice: {maleVoice.VoiceInfo.Name} " +
                        $"({maleVoice.VoiceInfo.Gender}, {maleVoice.VoiceInfo.Culture.Name})");
                }
                else
                {
                    Debug.WriteLine(
                        "No enabled male Windows TTS voice is installed; " +
                        "using the system default voice for EAS playback.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Unable to select a male EAS TTS voice; using system default: {ex}");
            }
        }

        private void SpeechSynthesizer_SpeakCompleted(
            object? sender,
            SpeakCompletedEventArgs e)
        {
            MarkSpeechFinished();
        }

        private void MarkSpeechFinished()
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(MarkSpeechFinished));
                }
                catch (InvalidOperationException)
                {
                }
                return;
            }

            _speechFinished = true;
            TryFinishAlert();
        }

        private void panel1_Paint(object? sender, PaintEventArgs e)
        {
            if (_alert == null)
                return;

            e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            using SolidBrush brush = new(lblAlertText.ForeColor);
            using StringFormat format =
                new(StringFormat.GenericTypographic)
                {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Near,
                    FormatFlags = StringFormatFlags.NoWrap
                };

            e.Graphics.DrawString(
                _alert.DisplayText,
                lblAlertText.Font,
                brush,
                new RectangleF(
                    (float)_scrollX,
                    0,
                    Math.Max(_messageWidth, 1f),
                    panel1.ClientSize.Height),
                format);
        }

        private void panel1_Resize(object? sender, EventArgs e) =>
            panel1.Invalidate();

        private void TryFinishAlert()
        {
            if (_finishedRaised || !_completedOneScroll || !_speechFinished)
                return;

            _finishedRaised = true;
            StopAnimation();
            AlertFinished?.Invoke(this, EventArgs.Empty);
        }

        private void ctrlEmergencyTicker_Disposed(object? sender, EventArgs e)
        {
            StopAnimation();
            _animationTimer.Dispose();
            _scrollClock.Stop();

            if (_timerResolutionRequested)
            {
                TimeEndPeriod(TimerResolutionMilliseconds);
                _timerResolutionRequested = false;
            }

            if (_speechSynthesizer != null)
            {
                try
                {
                    _speechSynthesizer.SpeakCompleted -= SpeechSynthesizer_SpeakCompleted;
                    _speechSynthesizer.SpeakAsyncCancelAll();
                    _speechSynthesizer.Dispose();
                }
                catch
                {
                }
                finally
                {
                    _speechSynthesizer = null;
                }
            }
        }
    }
}
