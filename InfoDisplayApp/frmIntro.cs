using LibVLCSharp.Shared;
using System.Diagnostics;

namespace InfoDisplayApp.Properties
{
    public partial class frmIntro : Form
    {
        private LibVLC? _libVlc;
        private MediaPlayer? _mediaPlayer;
        private Media? _media;
        private bool _completed;

        public event EventHandler? IntroCompleted;

        public frmIntro()
        {
            InitializeComponent();
        }

        private void frmIntro_Load(object sender, EventArgs e)
        {
            Cursor.Hide();
            BringToFront();
            try
            {
                Core.Initialize();

                _libVlc = new LibVLC("--no-video-title-show");
                _mediaPlayer = new MediaPlayer(_libVlc);

                // The intro carries its own soundtrack. Explicitly enable VLC audio
                // rather than relying on the MediaPlayer defaults or prior VLC state.
                _mediaPlayer.Mute = false;
                _mediaPlayer.Volume = 100;
                _mediaPlayer.Fullscreen = true;

                videoView.MediaPlayer = _mediaPlayer;

                _mediaPlayer.EndReached += MediaPlayer_EndReached;
                _mediaPlayer.EncounteredError += MediaPlayer_EncounteredError;
                _mediaPlayer.Playing += MediaPlayer_Playing;

                string introPath = Path.Combine(
                    AppContext.BaseDirectory, "Resources", "intro.mov");

                if (!File.Exists(introPath))
                    throw new FileNotFoundException("The InfoScreen intro video was not found.", introPath);

                _media = new Media(_libVlc, introPath, FromType.FromPath);

                if (!_mediaPlayer.Play(_media))
                    throw new InvalidOperationException("VLC could not start the InfoScreen intro video.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Intro playback failed: {ex}");
                CompleteIntro();
            }
        }

        private void MediaPlayer_Playing(object? sender, EventArgs e)
        {
            if (IsDisposed || Disposing)
                return;

            if (InvokeRequired)
            {
                BeginInvoke((Action)(() =>
                {
                    if (!IsDisposed && !Disposing)
                        BringToFront();
                }));
                return;
            }

            BringToFront();
        }

        private void MediaPlayer_EndReached(object? sender, EventArgs e)
        {
            CompleteIntro();
        }

        private void MediaPlayer_EncounteredError(object? sender, EventArgs e)
        {
            Debug.WriteLine("VLC reported an error while playing the InfoScreen intro.");
            CompleteIntro();
        }

        private void CompleteIntro()
        {
            if (_completed || IsDisposed)
                return;

            if (InvokeRequired)
            {
                BeginInvoke((Action)CompleteIntro);
                return;
            }

            if (_completed)
                return;

            _completed = true;
            IntroCompleted?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.EndReached -= MediaPlayer_EndReached;
                _mediaPlayer.EncounteredError -= MediaPlayer_EncounteredError;
                _mediaPlayer.Playing -= MediaPlayer_Playing;
                _mediaPlayer.Stop();
            }

            videoView.MediaPlayer = null;
            _media?.Dispose();
            _mediaPlayer?.Dispose();
            _libVlc?.Dispose();

            base.OnFormClosed(e);
        }
    }
}
