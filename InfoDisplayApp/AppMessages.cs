using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace InfoDisplayApp
{
    public enum AppMessageType
    {
        Info,
        Warning,
        Error,
        Question
    }

    public sealed class AppMessageEventArgs : EventArgs
    {
        public AppMessageEventArgs(AppMessageType type, string message, Exception? exception = null)
        {
            Type = type;
            Message = message;
            Exception = exception;
            Timestamp = DateTime.Now;
        }

        public AppMessageType Type { get; }
        public string Message { get; }
        public Exception? Exception { get; }
        public DateTime Timestamp { get; }
    }

    public static class AppMessages
    {
        private static SynchronizationContext? _uiContext;
        private static Form? _owner;
        private static bool _initialized;
        private static bool _publishing;
        private static volatile bool _offlineMode;

        /// <summary>
        /// When true, network-related warning/error windows are suppressed while
        /// Offline Mode owns the user-facing explanation. Diagnostics are still logged.
        /// </summary>
        public static bool OfflineMode
        {
            get => _offlineMode;
            set => _offlineMode = value;
        }

        public static event EventHandler<AppMessageEventArgs>? MessageRaised;

        public static void Initialize(Form owner)
        {
            _owner = owner;
            _uiContext = SynchronizationContext.Current;

            if (_initialized)
                return;

            _initialized = true;

            Application.ThreadException += (_, e) =>
                Error("An unhandled application error occurred.", e.Exception);

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                Exception? exception = e.ExceptionObject as Exception;
                Error(
                    e.IsTerminating
                        ? "A fatal unhandled application error occurred."
                        : "An unhandled application error occurred.",
                    exception);
            };

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                // A transient socket timeout can be reported late by the runtime
                // after the originating operation has already handled the outage.
                // Keep it in diagnostics, but don't interrupt TV/dashboard use
                // with a duplicate modal error window.
                if (ContainsSocketTimeout(e.Exception))
                {
                    Debug.WriteLine(
                        $"Observed late background network timeout: {e.Exception}");
                    e.SetObserved();
                    return;
                }

                Error("An unobserved background task error occurred.", e.Exception);
                e.SetObserved();
            };

            Trace.Listeners.Add(new AppMessageTraceListener());
        }

        private static bool ContainsSocketTimeout(Exception exception)
        {
            if (exception is System.Net.Sockets.SocketException socketException &&
                socketException.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut)
            {
                return true;
            }

            if (exception is AggregateException aggregate)
                return aggregate.Flatten().InnerExceptions.Any(ContainsSocketTimeout);

            return exception.InnerException != null &&
                   ContainsSocketTimeout(exception.InnerException);
        }

        public static void Info(string message) =>
            Raise(AppMessageType.Info, message, null);

        public static void Warning(string message) =>
            Raise(AppMessageType.Warning, message, null);

        public static void Error(string message, Exception? exception = null) =>
            Raise(AppMessageType.Error, message, exception);

        /// <summary>
        /// Shows a modal Yes/No question using InfoDisplay's custom message
        /// window and returns the user's choice. The call is marshalled to the
        /// UI thread when invoked from background work.
        /// </summary>
        public static DialogResult Question(
            string message,
            string yesText = "Yes",
            string noText = "No")
        {
            if (string.IsNullOrWhiteSpace(message))
                return DialogResult.No;

            if (_owner == null || _owner.IsDisposed)
                return DialogResult.No;

            DialogResult ShowQuestion()
            {
                try
                {
                    frmMessageWindow window = new();
                    window.SetMessage(message);
                    window.SetQuestionMode(yesText, noText);
                    window.TopMost = true;

                    MessageRaised?.Invoke(
                        null,
                        new AppMessageEventArgs(AppMessageType.Question, message));

                    Debug.WriteLine($"[Question] {message}");
                    return window.ShowDialog(_owner);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to show in-app question window: {ex}");
                    return DialogResult.No;
                }
            }

            if (_owner.InvokeRequired)
            {
                try
                {
                    return (DialogResult)_owner.Invoke(new Func<DialogResult>(ShowQuestion));
                }
                catch (InvalidOperationException)
                {
                    // ObjectDisposedException derives from InvalidOperationException,
                    // so this covers both a disposed owner and an invalid invoke state.
                    return DialogResult.No;
                }
            }

            return ShowQuestion();
        }

        public static bool AskYesNo(
            string message,
            string yesText = "Yes",
            string noText = "No") =>
            Question(message, yesText, noText) == DialogResult.Yes;

        public static void ReportException(Exception exception, string? context = null)
        {
            string message = string.IsNullOrWhiteSpace(context)
                ? exception.Message
                : $"{context}: {exception.Message}";

            Error(message, exception);
        }

        internal static void FromDiagnosticOutput(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || _publishing)
                return;

            string trimmed = message.Trim();
            string lower = trimmed.ToLowerInvariant();

            // Ping diagnostics are status/telemetry, never something that should
            // interrupt the TV/dashboard with a modal message window.
            if (lower.Contains("ping"))
                return;

            // The battery-powered Tapo doorbell normally stops answering pings
            // while asleep. That is expected behavior and should remain a ticker
            // status only, not interrupt the TV/dashboard with a warning window.
            if (lower.Contains("doorbell camera") &&
                lower.Contains("offline, possibly asleep"))
            {
                return;
            }

            AppMessageType? type = null;

            if (lower.Contains("exception") ||
                lower.Contains("error") ||
                lower.Contains("failed") ||
                lower.Contains("failure") ||
                lower.Contains("encountered"))
            {
                type = AppMessageType.Error;
            }
            else if (lower.Contains("warning") ||
                     lower.Contains("warn") ||
                     lower.Contains("unavailable") ||
                     lower.Contains("offline") ||
                     lower.Contains("timeout") ||
                     lower.Contains("cancelled") ||
                     lower.Contains("canceled"))
            {
                type = AppMessageType.Warning;
            }

            if (type.HasValue)
                Raise(type.Value, trimmed, null, echoToDebug: false);
        }

        private static void Raise(
            AppMessageType type,
            string message,
            Exception? exception,
            bool echoToDebug = true)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            if (_publishing)
                return;

            _publishing = true;

            try
            {
                if (echoToDebug)
                {
                    string logText = exception == null
                        ? $"[{type}] {message}"
                        : $"[{type}] {message}{Environment.NewLine}{exception}";

                    Debug.WriteLine(logText);
                }

                AppMessageEventArgs args = new(type, message, exception);

                if (type == AppMessageType.Warning || type == AppMessageType.Error)
                    WriteMessageLog(args);

                bool suppressOfflineNetworkMessage =
                    (type == AppMessageType.Warning || type == AppMessageType.Error) &&
                    LooksNetworkRelated(message, exception) &&
                    (_offlineMode || LooksLikePingFailure(message, exception));

                if (suppressOfflineNetworkMessage)
                {
                    Debug.WriteLine($"OFFLINE MODE: suppressed network message window: {message}");
                    return;
                }

                MessageRaised?.Invoke(null, args);

                void ShowWindow(object? _)
                {
                    try
                    {
                        if (_owner == null || _owner.IsDisposed)
                            return;

                        string detail = exception == null
                            ? message
                            : $"{message}{Environment.NewLine}{Environment.NewLine}{exception.Message}";

                        frmMessageWindow window = new();
                        window.SetIcon(type switch
                        {
                            AppMessageType.Warning => "warning",
                            AppMessageType.Error => "error",
                            AppMessageType.Question => "question",
                            _ => "info"
                        });
                        window.SetMessage(detail);
                        window.TopMost = true;
                        window.Show(_owner);
                    }
                    catch (Exception showException)
                    {
                        Debug.WriteLine($"Failed to show in-app message window: {showException}");
                    }
                }

                if (_uiContext != null)
                    _uiContext.Post(ShowWindow, null);
                else
                    ShowWindow(null);
            }
            finally
            {
                _publishing = false;
            }
        }
        private static bool LooksLikePingFailure(string message, Exception? exception)
        {
            string lower = message.ToLowerInvariant();
            return lower.Contains("ping failed") ||
                   lower.Contains("ping failure") ||
                   (lower.Contains("ping") && lower.Contains("unreachable")) ||
                   (exception is System.Net.NetworkInformation.PingException);
        }

        private static bool LooksNetworkRelated(string message, Exception? exception)
        {
            static bool NetworkException(Exception? ex)
            {
                if (ex == null) return false;
                if (ex is HttpRequestException ||
                    ex is System.Net.Sockets.SocketException ||
                    ex is System.Net.WebException ||
                    ex is TimeoutException ||
                    ex is TaskCanceledException)
                    return true;

                if (ex is AggregateException aggregate &&
                    aggregate.Flatten().InnerExceptions.Any(NetworkException))
                    return true;

                return NetworkException(ex.InnerException);
            }

            if (NetworkException(exception))
                return true;

            string lower = message.ToLowerInvariant();
            return lower.Contains("internet") ||
                   lower.Contains("network") ||
                   lower.Contains("http") ||
                   lower.Contains("repository") ||
                   lower.Contains("weather") ||
                   lower.Contains("remote") ||
                   lower.Contains("connection") ||
                   lower.Contains("timed out") ||
                   lower.Contains("timeout") ||
                   lower.Contains("unable to reach") ||
                   lower.Contains("nws") ||
                   lower.Contains("forecast") ||
                   lower.Contains("ping") ||
                   lower.Contains("tapo") ||
                   lower.Contains("camera") ||
                   lower.Contains("rycraft") ||
                   lower.Contains("rcon") ||
                   lower.Contains("minecraft") ||
                   lower.Contains("minestat");
        }

        private static void WriteMessageLog(AppMessageEventArgs args)
        {
            try
            {
                string logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
                Directory.CreateDirectory(logsDirectory);

                string level = args.Type == AppMessageType.Error ? "ERROR" : "ALERT";
                string timestamp = args.Timestamp.ToString("hh-mm-ss-tt-MM-dd-yyyy");
                string filePath = Path.Combine(
                    logsDirectory,
                    $"InfoScreen-{level}-{timestamp}.txt");

                StringBuilder contents = new();
                contents.AppendLine($"InfoScreen {level}");
                contents.AppendLine($"Time: {args.Timestamp:yyyy-MM-dd HH:mm:ss}");
                contents.AppendLine();
                contents.AppendLine(args.Message);

                if (args.Exception != null)
                {
                    contents.AppendLine();
                    contents.AppendLine("Exception details:");
                    contents.AppendLine(args.Exception.ToString());
                }

                File.WriteAllText(filePath, contents.ToString(), Encoding.UTF8);
            }
            catch (Exception logException)
            {
                // Logging must never cause another user-facing error, otherwise
                // a failed log write could recursively create more messages.
                Debug.WriteLine($"Failed to write InfoScreen message log: {logException}");
            }
        }
    }

    internal sealed class AppMessageTraceListener : TraceListener
    {
        private readonly object _sync = new();
        private readonly StringBuilder _buffer = new();

        public override void Write(string? message)
        {
            if (message == null)
                return;

            lock (_sync)
            {
                _buffer.Append(message);
            }
        }

        public override void WriteLine(string? message)
        {
            lock (_sync)
            {
                if (!string.IsNullOrEmpty(message))
                    _buffer.Append(message);

                string completed = _buffer.ToString();
                _buffer.Clear();

                if (!string.IsNullOrWhiteSpace(completed))
                    AppMessages.FromDiagnosticOutput(completed);
            }
        }
    }
}
