namespace InfoDisplayApp.Services
{
    internal static class AudioEndpointDiagnostics
    {
        private static readonly object Sync = new();
        private static AudioEndpointMonitor? _monitor;

        public static void Start()
        {
            if (!AppSettings.Current.Diagnostics.CoreAudioMonitor)
                return;

            lock (Sync)
            {
                if (_monitor != null)
                    return;

                try
                {
                    _monitor = new AudioEndpointMonitor();
                    _monitor.Start();
                }
                catch (Exception ex)
                {
                    AudioPathology.Log($"COREAUDIO: endpoint monitor failed to start: {ex}");
                    _monitor?.Dispose();
                    _monitor = null;
                }
            }
        }

        public static void Stop()
        {
            lock (Sync)
            {
                if (_monitor == null)
                    return;

                _monitor.Dispose();
                _monitor = null;
            }
        }
    }
}
