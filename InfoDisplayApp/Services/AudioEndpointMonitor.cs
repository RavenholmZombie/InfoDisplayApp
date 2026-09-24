using NAudio.CoreAudioApi;

namespace InfoDisplayApp.Services
{
    internal sealed class AudioEndpointMonitor : MMNotificationClient, IDisposable
    {
        private readonly MMDeviceEnumerator _enumerator = new();
        private bool _registered;

        public void Start()
        {
            if (_registered)
                return;

            AudioPathology.Log("COREAUDIO: endpoint monitor starting.");
            LogCurrentDefaults("initial");

            _enumerator.RegisterEndpointNotificationCallback(this);
            _registered = true;
            AudioPathology.Log("COREAUDIO: endpoint notification callback registered.");
        }

        public void Dispose()
        {
            if (_registered)
            {
                try
                {
                    _enumerator.UnregisterEndpointNotificationCallback(this);
                    AudioPathology.Log("COREAUDIO: endpoint notification callback unregistered.");
                }
                catch (Exception ex)
                {
                    AudioPathology.Log($"COREAUDIO: callback unregister failed: {ex}");
                }

                _registered = false;
            }

            _enumerator.Dispose();
        }

        public override void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            AudioPathology.Log(
                $"COREAUDIO EVENT: DefaultDeviceChanged flow={flow}; role={role}; id='{defaultDeviceId}'.");
            LogDevice("new default", defaultDeviceId);
            LogCurrentDefaults("after DefaultDeviceChanged");
        }

        public override void OnDeviceAdded(string pwstrDeviceId)
        {
            AudioPathology.Log($"COREAUDIO EVENT: DeviceAdded id='{pwstrDeviceId}'.");
            LogDevice("added", pwstrDeviceId);
        }

        public override void OnDeviceRemoved(string deviceId)
        {
            AudioPathology.Log($"COREAUDIO EVENT: DeviceRemoved id='{deviceId}'.");
            LogCurrentDefaults("after DeviceRemoved");
        }

        public override void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            AudioPathology.Log(
                $"COREAUDIO EVENT: DeviceStateChanged id='{deviceId}'; newState={newState}.");
            LogDevice("state changed", deviceId);
        }

        public override void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
            AudioPathology.Log(
                $"COREAUDIO EVENT: PropertyValueChanged id='{pwstrDeviceId}'; " +
                $"property={key.formatId}/{key.propertyId}.");
            LogDevice("property changed", pwstrDeviceId);
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
            try { return device.AudioClient.MixFormat.ToString(); }
            catch (Exception ex) { return $"<unavailable: {ex.GetType().Name}>"; }
        }
    }
}
