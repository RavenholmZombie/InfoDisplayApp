using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace InfoDisplayApp
{
    public enum AppMessageType
    {
        Info,
        Warning,
        Error
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
                Error("An unobserved background task error occurred.", e.Exception);
                e.SetObserved();
            };
        }

        public static void Info(string message) =>
            Raise(AppMessageType.Info, message, null);

        public static void Warning(string message) =>
            Raise(AppMessageType.Warning, message, null);

        public static void Error(string message, Exception? exception = null) =>
            Raise(AppMessageType.Error, message, exception);

        public static void ReportException(Exception exception, string? context = null)
        {
            string message = string.IsNullOrWhiteSpace(context)
                ? exception.Message
                : $"{context}: {exception.Message}";

            Error(message, exception);
        }

        private static void Raise(
            AppMessageType type,
            string message,
            Exception? exception)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            string logText = exception == null
                ? $"[{type}] {message}"
                : $"[{type}] {message}{Environment.NewLine}{exception}";

            Debug.WriteLine(logText);

            AppMessageEventArgs args = new(type, message, exception);
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
    }
}
