using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace InfoDisplayApp.Properties
{
    public partial class ctrlTicker : UserControl
    {
        private readonly System.Windows.Forms.Timer _scrollTimer;
        private readonly System.Windows.Forms.Timer _reloadTimer;

        private readonly List<string> _messages = new();

        private int _currentMessageIndex = 0;
        private int _scrollX;

        // Number of pixels moved per timer tick.
        private const int ScrollSpeed = 10;

        // Space between the old message leaving and the next one entering.
        private const int MessageGap = 50;

        private string TickerPath =>
            Path.Combine(AppContext.BaseDirectory, "ticker.txt");

        public ctrlTicker()
        {
            InitializeComponent();

            //
            // IMPORTANT:
            // lblTextTicker must NOT be DockStyle.Fill because
            // we're going to move it horizontally.
            //
            lblTextTicker.Dock = DockStyle.None;
            lblTextTicker.AutoSize = true;
            lblTextTicker.TextAlign = ContentAlignment.MiddleLeft;

            _scrollTimer = new System.Windows.Forms.Timer
            {
                // Roughly 60 FPS
                Interval = 16
            };

            _scrollTimer.Tick += ScrollTimer_Tick;

            _reloadTimer = new System.Windows.Forms.Timer
            {
                // Check ticker.txt every 10 seconds.
                Interval = 10_000
            };

            _reloadTimer.Tick += ReloadTimer_Tick;

            Load += ctrlTicker_Load;
            Disposed += ctrlTicker_Disposed;
        }

        private void ctrlTicker_Load(object? sender, EventArgs e)
        {
            LoadTickerMessages();

            if (_messages.Count > 0)
            {
                ShowCurrentMessage();
                _scrollTimer.Start();
            }

            _reloadTimer.Start();
        }

        private void LoadTickerMessages()
        {
            try
            {
                if (!File.Exists(TickerPath))
                {
                    Debug.WriteLine(
                        $"Ticker file not found: {TickerPath}");

                    _messages.Clear();
                    lblTextTicker.Text = "";

                    return;
                }

                string[] lines = File.ReadAllLines(TickerPath);

                List<string> newMessages = lines
                    .Select(line => line.Trim())
                    .Where(line =>
                        !string.IsNullOrWhiteSpace(line) &&
                        !line.StartsWith("#"))
                    .ToList();

                if (newMessages.Count == 0)
                {
                    _messages.Clear();
                    lblTextTicker.Text = "";

                    return;
                }

                //
                // Only rebuild the ticker if the file's
                // actual contents have changed.
                //
                if (_messages.SequenceEqual(newMessages))
                    return;

                _messages.Clear();
                _messages.AddRange(newMessages);

                _currentMessageIndex = 0;

                ShowCurrentMessage();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Unable to load ticker.txt: {ex}");
            }
        }

        private void ShowCurrentMessage()
        {
            if (_messages.Count == 0)
            {
                lblTextTicker.Text = "";
                return;
            }

            if (_currentMessageIndex >= _messages.Count)
                _currentMessageIndex = 0;

            lblTextTicker.Text =
                _messages[_currentMessageIndex];

            //
            // Resize label to fit this particular message.
            //
            Size preferredSize = TextRenderer.MeasureText(
                lblTextTicker.Text,
                lblTextTicker.Font);

            lblTextTicker.Size = new Size(
                preferredSize.Width + 10,
                panel1.ClientSize.Height);

            //
            // Begin just beyond the right edge of the ticker.
            //
            _scrollX =
                panel1.ClientSize.Width + MessageGap;

            lblTextTicker.Location = new Point(
                _scrollX,
                0);
        }

        private void ScrollTimer_Tick(
            object? sender,
            EventArgs e)
        {
            if (_messages.Count == 0)
                return;

            _scrollX -= ScrollSpeed;

            lblTextTicker.Left = _scrollX;

            //
            // Has the entire message left the screen?
            //
            if (lblTextTicker.Right < 0)
            {
                _currentMessageIndex++;

                if (_currentMessageIndex >= _messages.Count)
                    _currentMessageIndex = 0;

                ShowCurrentMessage();
            }
        }

        private void ReloadTimer_Tick(
            object? sender,
            EventArgs e)
        {
            LoadTickerMessages();

            if (_messages.Count > 0 &&
                !_scrollTimer.Enabled)
            {
                ShowCurrentMessage();
                _scrollTimer.Start();
            }

            if (_messages.Count == 0)
            {
                _scrollTimer.Stop();
            }
        }

        private void ctrlTicker_Disposed(
            object? sender,
            EventArgs e)
        {
            _scrollTimer.Stop();
            _reloadTimer.Stop();

            _scrollTimer.Dispose();
            _reloadTimer.Dispose();
        }
    }
}