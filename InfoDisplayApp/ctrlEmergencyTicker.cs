using InfoDisplayApp.Properties;
using InfoDisplayApp.Services;
using System.Diagnostics;
using System.Drawing.Text;
using System.Media;
using System.Speech.Synthesis;

namespace InfoDisplayApp
{
    public partial class ctrlEmergencyTicker : UserControl
    {
        private readonly System.Windows.Forms.Timer _animationTimer;
        private readonly Stopwatch _scrollClock = new();
        private SpeechSynthesizer? _speechSynthesizer;
        private NwsAlertMessage? _alert;
        private double _lastScrollSeconds;
        private double _scrollX;
        private float _messageWidth;
        private bool _completedOneScroll;
        private bool _speechFinished;
        private bool _finishedRaised;

        private const double ScrollPixelsPerSecond = 190.0;
        private const int MessageGap = 40;
        private const int AnimationIntervalMilliseconds = 16;

        public event EventHandler? AlertFinished;

        public ctrlEmergencyTicker()
        {
            InitializeComponent();

            lblAlertText.Visible = false;
            panel1.Paint += panel1_Paint;
            panel1.Resize += panel1_Resize;

            _animationTimer = new System.Windows.Forms.Timer
            {
                Interval = AnimationIntervalMilliseconds
            };
            _animationTimer.Tick += AnimationTimer_Tick;

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
            _animationTimer.Start();
            panel1.Invalidate();

            _ = BeginAlertAudioAsync();
        }

        private void ctrlEmergencyTicker_Load(object sender, EventArgs e)
        {
            // Alert playback is started explicitly by frmMain once the control
            // has been placed on screen and application audio has been muted.
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

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (_alert == null || _finishedRaised)
                return;

            double now = _scrollClock.Elapsed.TotalSeconds;
            double elapsed = Math.Clamp(now - _lastScrollSeconds, 0.0, 0.050);
            _lastScrollSeconds = now;

            _scrollX -= ScrollPixelsPerSecond * elapsed;
            panel1.Invalidate();

            if (_scrollX + _messageWidth < -MessageGap)
            {
                _completedOneScroll = true;

                if (_speechFinished)
                {
                    TryFinishAlert();
                }
                else
                {
                    // Keep the message on screen while TTS is still reading it.
                    // The alert ends only after at least one complete visual pass
                    // and the spoken message have both finished.
                    ResetScrollPosition();
                }
            }
        }

        private void ResetScrollPosition()
        {
            _scrollX = panel1.ClientSize.Width + MessageGap;
            _scrollClock.Restart();
            _lastScrollSeconds = 0;
        }

        private void panel1_Paint(object? sender, PaintEventArgs e)
        {
            if (_alert == null)
                return;

            e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            using SolidBrush brush = new(Color.White);
            using StringFormat format = new(StringFormat.GenericTypographic)
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

        private void panel1_Resize(object? sender, EventArgs e)
        {
            if (_alert != null && !_finishedRaised)
                panel1.Invalidate();
        }

        private void TryFinishAlert()
        {
            if (_finishedRaised || !_completedOneScroll || !_speechFinished)
                return;

            _finishedRaised = true;
            _animationTimer.Stop();
            AlertFinished?.Invoke(this, EventArgs.Empty);
        }

        private void ctrlEmergencyTicker_Disposed(object? sender, EventArgs e)
        {
            _animationTimer.Stop();
            _animationTimer.Dispose();
            _scrollClock.Stop();

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
