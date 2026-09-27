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
            try
            {
                Core.Initialize();

                _libVlc = new LibVLC("--no-video-title-show");
                _mediaPlayer = new MediaPlayer(_libVlc);
                videoView.MediaPlayer = _mediaPlayer;

                _mediaPlayer.EndReached += MediaPlayer_EndReached;
                _mediaPlayer.EncounteredError += MediaPlayer_EncounteredError;

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
                BeginInvoke(CompleteIntro);
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
