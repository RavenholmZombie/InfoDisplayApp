using InfoDisplayApp.Properties;
using System;
using System.Drawing;
using System.Media;
using System.Windows.Forms;

namespace InfoDisplayApp
{
    public partial class frmMessageWindow : Form
    {
        private string _messageText = "";
        private string _messageType = "info";
        private const int AutoDismissSeconds = 10;
        private readonly System.Windows.Forms.Timer _autoDismissTimer = new();
        private int _secondsRemaining = AutoDismissSeconds;

        public frmMessageWindow()
        {
            InitializeComponent();
            _autoDismissTimer.Interval = 1000;
            _autoDismissTimer.Tick += AutoDismissTimer_Tick;
        }

        private void frmMessageWindow_Load(object sender, EventArgs e)
        {
            if (!_messageType.Equals("question", StringComparison.OrdinalIgnoreCase))
                StartAutoDismiss();
        }

        private void StartAutoDismiss()
        {
            _secondsRemaining = AutoDismissSeconds;
            lblAutoDismiss.Text = $"Closing in {_secondsRemaining} seconds...";
            lblAutoDismiss.Visible = true;
            _autoDismissTimer.Start();
        }

        private void AutoDismissTimer_Tick(object? sender, EventArgs e)
        {
            _secondsRemaining--;

            if (_secondsRemaining <= 0)
            {
                _autoDismissTimer.Stop();
                Close();
                return;
            }

            lblAutoDismiss.Text = $"Closing in {_secondsRemaining} seconds...";
        }

        private void PlaySoundForMessageType(string messageType)
        {
            switch (messageType.ToLowerInvariant())
            {
                case "info":
                    using (SoundPlayer player = new SoundPlayer(Resources.sfx_information))
                    {
                        player.Load();
                        player.Play();
                    }
                    break;
                case "warning":
                    using (SoundPlayer player = new SoundPlayer(Resources.sfx_alarm))
                    {
                        player.Load();
                        player.Play();
                    }
                    break;
                case "error":
                    using (SoundPlayer player = new SoundPlayer(Resources.sfx_error))
                    {
                        player.Load();
                        player.Play();
                    }
                    break;
                case "question":
                    using (SoundPlayer player = new SoundPlayer(Resources.sfx_question))
                    {
                        player.Load();
                        player.Play();
                    }
                    break;
                default:
                    using (SoundPlayer player = new SoundPlayer(Resources.sfx_information))
                    {
                        player.Load();
                        player.Play();
                    }
                    break;
            }
        }

        public string SetMessage(string messageText)
        {
            _messageText = messageText;
            rtbMessage.Text = _messageText;
            return _messageText;
        }

        public string SetIcon(string messageType)
        {
            _messageType = messageType;

            switch (messageType.ToLowerInvariant())
            {
                case "info":
                    pboxIcn.BackgroundImage = Properties.Resources.info_icn;
                    PlaySoundForMessageType("info");
                    break;

                case "warning":
                    pboxIcn.BackgroundImage = Properties.Resources.alert_icn_msg;
                    PlaySoundForMessageType("warning");
                    break;

                case "error":
                    pboxIcn.BackgroundImage = Properties.Resources.error_icn;
                    PlaySoundForMessageType("error");
                    break;

                case "question":
                    pboxIcn.BackgroundImage = SystemIcons.Question.ToBitmap();
                    PlaySoundForMessageType("question");
                    break;

                default:
                    pboxIcn.BackgroundImage = Properties.Resources.info_icn;
                    PlaySoundForMessageType("info");
                    break;
            }

            return _messageType;
        }

        /// <summary>
        /// Converts the message window from its normal Dismiss mode into a
        /// modal Yes/No question prompt.
        /// </summary>
        public void SetQuestionMode(string yesText = "Yes", string noText = "No")
        {
            SetIcon("question");

            _autoDismissTimer.Stop();
            lblAutoDismiss.Visible = false;
            btnClose.Visible = false;

            btnYes.Text = string.IsNullOrWhiteSpace(yesText) ? "Yes" : yesText;
            btnNo.Text = string.IsNullOrWhiteSpace(noText) ? "No" : noText;
            btnYes.Visible = true;
            btnNo.Visible = true;

            btnYes.DialogResult = DialogResult.Yes;
            btnNo.DialogResult = DialogResult.No;

            AcceptButton = btnYes;
            CancelButton = btnNo;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            _autoDismissTimer.Stop();
            Close();
        }

        private void btnYes_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Yes;
            Close();
        }

        private void btnNo_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.No;
            Close();
        }
    }
}
