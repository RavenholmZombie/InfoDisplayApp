using NAudio.CoreAudioApi;

namespace InfoDisplayApp.Services
{
    internal sealed class AudioEndpointMonitor : IDisposable
    {
        private readonly MMDeviceEnumerator _enumerator = new();
        private readonly MMDeviceNotificationClient _notifications;

        public AudioEndpointMonitor()
        {
            // NAudio 3.1 moved the raw IMMNotificationClient API internal.
            // CreateNotificationClient is the supported public notification surface.
            _notifications = _enumerator.CreateNotificationClient(useSynchronizationContext: false);
        }

        public void Start()
        {
            AudioPathology.Log("COREAUDIO: endpoint monitor starting.");
            LogCurrentDefaults("initial");

            _notifications.DefaultDeviceChanged += Notifications_DefaultDeviceChanged;
            _notifications.DeviceAdded += Notifications_DeviceAdded;
            _notifications.DeviceRemoved += Notifications_DeviceRemoved;
            _notifications.DeviceStateChanged += Notifications_DeviceStateChanged;
            _notifications.PropertyValueChanged += Notifications_PropertyValueChanged;

            AudioPathology.Log("COREAUDIO: endpoint notification events subscribed.");
        }

        public void Dispose()
        {
            _notifications.DefaultDeviceChanged -= Notifications_DefaultDeviceChanged;
            _notifications.DeviceAdded -= Notifications_DeviceAdded;
            _notifications.DeviceRemoved -= Notifications_DeviceRemoved;
            _notifications.DeviceStateChanged -= Notifications_DeviceStateChanged;
            _notifications.PropertyValueChanged -= Notifications_PropertyValueChanged;
            _notifications.Dispose();
            _enumerator.Dispose();
            AudioPathology.Log("COREAUDIO: endpoint notification monitor disposed.");
        }

        private void Notifications_DefaultDeviceChanged(object? sender, DefaultDeviceChangedEventArgs e)
        {
            AudioPathology.Log(
                $"COREAUDIO EVENT: DefaultDeviceChanged flow={e.Flow}; role={e.Role}; id='{e.DeviceId}'.");
            LogDevice("new default", e.DeviceId);
            LogCurrentDefaults("after DefaultDeviceChanged");
        }

        private void Notifications_DeviceAdded(object? sender, DeviceNotificationEventArgs e)
        {
            AudioPathology.Log($"COREAUDIO EVENT: DeviceAdded id='{e.DeviceId}'.");
            LogDevice("added", e.DeviceId);
        }

        private void Notifications_DeviceRemoved(object? sender, DeviceNotificationEventArgs e)
        {
            AudioPathology.Log($"COREAUDIO EVENT: DeviceRemoved id='{e.DeviceId}'.");
            LogCurrentDefaults("after DeviceRemoved");
        }

        private void Notifications_DeviceStateChanged(object? sender, DeviceStateChangedEventArgs e)
        {
            AudioPathology.Log(
                $"COREAUDIO EVENT: DeviceStateChanged id='{e.DeviceId}'; newState={e.NewState}.");
            LogDevice("state changed", e.DeviceId);
        }

        private void Notifications_PropertyValueChanged(object? sender, DevicePropertyChangedEventArgs e)
        {
            AudioPathology.Log(
                $"COREAUDIO EVENT: PropertyValueChanged id='{e.DeviceId}'; " +
                $"property={e.PropertyKey.formatId}/{e.PropertyKey.propertyId}.");
            LogDevice("property changed", e.DeviceId);
        }

        private void LogCurrentDefaults(string reason)
        {
            foreach (DataFlow flow in new[] { DataFlow.Render, DataFlow.Capture })
            {
                foreach (Role role in new[] { Role.Console, Role.Multimedia, Role.Communications })
                {
                    try
                    {
                        using MMDevice device = _enumerator.GetDefaultAudioEndpoint(flow, role);
                        AudioPathology.Log(
                            $"COREAUDIO DEFAULT ({reason}): flow={flow}; role={role}; " +
                            $"id='{device.ID}'; name='{SafeName(device)}'; state={device.State}; " +
                            $"format='{SafeFormat(device)}'.");
                    }
                    catch (Exception ex)
                    {
                        AudioPathology.Log(
                            $"COREAUDIO DEFAULT ({reason}): flow={flow}; role={role}; unavailable: " +
                            $"{ex.GetType().Name}: {ex.Message}");
                    }
                }
            }
        }

        private void LogDevice(string reason, string deviceId)
        {
            try
            {
                using MMDevice device = _enumerator.GetDevice(deviceId);
                AudioPathology.Log(
                    $"COREAUDIO DEVICE ({reason}): id='{device.ID}'; name='{SafeName(device)}'; " +
                    $"state={device.State}; format='{SafeFormat(device)}'.");
            }
            catch (Exception ex)
            {
                AudioPathology.Log(
                    $"COREAUDIO DEVICE ({reason}): id='{deviceId}'; lookup failed: " +
                    $"{ex.GetType().Name}: {ex.Message}");
            }
        }

        private static string SafeName(MMDevice device)
        {
            try { return device.FriendlyName; }
            catch { return "<unavailable>"; }
        }

        private static string SafeFormat(MMDevice device)
        {
            try
            {
#pragma warning disable CS0618
                return device.AudioClient.MixFormat.ToString();
#pragma warning restore CS0618
            }
            catch (Exception ex) { return $"<unavailable: {ex.GetType().Name}>"; }
        }
    }
}
