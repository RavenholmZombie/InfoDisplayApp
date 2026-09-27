using NAudio.Wave;
using System.Diagnostics;
using System.Text;

namespace InfoDisplayApp.Services
{
    internal static class AudioPathology
    {
        private static readonly object LogLock = new();

        public static string LogPath =>
            Path.Combine(AppContext.BaseDirectory, "logs",
                $"InfoScreen-AUDIO-PATHOLOGY-{Environment.ProcessId}.log");

        public static void BeginSession()
        {
            Log("============================================================");
            Log($"Audio pathology session started. PID={Environment.ProcessId}; OS={Environment.OSVersion}; .NET={Environment.Version}");
        }

        public static void InspectWave(string specimen, byte[] wavBytes)
        {
            if (!AppSettings.Current.Diagnostics.AudioPathology)
                return;

            try
            {
                using MemoryStream stream = new(wavBytes, writable: false);
                using WaveFileReader reader = new(stream);

                Log($"{specimen}: resourceBytes={wavBytes.LongLength}; waveFormat={reader.WaveFormat}; " +
                    $"dataLength={reader.Length}; totalTime={reader.TotalTime.TotalMilliseconds:F1}ms; " +
                    $"initialPosition={reader.Position}.");

                LogRiffChunks(specimen, wavBytes);
            }
            catch (Exception ex)
            {
                Log($"{specimen}: WAV inspection FAILED: {ex}");
            }
        }

        public static void LogPlaybackStarted(
            string specimen,
            WaveFileReader reader,
            IWavePlayer output,
            Stopwatch clock)
        {
            if (!AppSettings.Current.Diagnostics.AudioPathology)
                return;

            Log($"{specimen}: PLAY called at +{clock.Elapsed.TotalMilliseconds:F1}ms; " +
                $"readerPosition={reader.Position}/{reader.Length}; currentTime={reader.CurrentTime.TotalMilliseconds:F1}ms; " +
                $"playbackState={output.PlaybackState}.");
        }

        public static void LogPlaybackStopped(
            string specimen,
            WaveFileReader reader,
            IWavePlayer output,
            Stopwatch clock,
            Exception? exception)
        {
            if (!AppSettings.Current.Diagnostics.AudioPathology)
                return;

            string exceptionText = exception == null
                ? "none"
                : $"{exception.GetType().FullName}: {exception.Message}";

            Log($"{specimen}: PlaybackStopped at +{clock.Elapsed.TotalMilliseconds:F1}ms; " +
                $"readerPosition={reader.Position}/{reader.Length}; currentTime={reader.CurrentTime.TotalMilliseconds:F1}ms; " +
                $"totalTime={reader.TotalTime.TotalMilliseconds:F1}ms; playbackState={output.PlaybackState}; " +
                $"exception={exceptionText}.");
        }

        public static void Log(string message)
        {
            if (!AppSettings.Current.Diagnostics.AudioPathology)
                return;

            string line = $"{DateTime.Now:O} {message}{Environment.NewLine}";
            Debug.Write(line);

            try
            {
                lock (LogLock)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                    File.AppendAllText(LogPath, line);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to write audio pathology log: {ex}");
            }
        }

        private static void LogRiffChunks(string specimen, byte[] bytes)
        {
            if (bytes.Length < 12)
            {
                Log($"{specimen}: RIFF scan: resource is shorter than a WAV header.");
                return;
            }

            string riff = Encoding.ASCII.GetString(bytes, 0, 4);
            uint declaredRiffSize = BitConverter.ToUInt32(bytes, 4);
            string wave = Encoding.ASCII.GetString(bytes, 8, 4);
            long declaredFileBytes = declaredRiffSize + 8L;

            Log($"{specimen}: RIFF header riff='{riff}' wave='{wave}' declaredFileBytes={declaredFileBytes}; " +
                $"actualFileBytes={bytes.LongLength}; trailingBytes={bytes.LongLength - declaredFileBytes}.");

            int offset = 12;
            int chunkIndex = 0;

            while (offset + 8 <= bytes.Length && chunkIndex < 64)
            {
                string id = Encoding.ASCII.GetString(bytes, offset, 4);
                uint size = BitConverter.ToUInt32(bytes, offset + 4);
                long dataStart = offset + 8L;
                long dataEnd = dataStart + size;

                Log($"{specimen}: RIFF chunk[{chunkIndex}] id='{Printable(id)}' headerOffset={offset}; " +
                    $"dataOffset={dataStart}; declaredSize={size}; declaredEnd={dataEnd}.");

                if (dataEnd > bytes.Length)
                {
                    Log($"{specimen}: RIFF WARNING chunk '{Printable(id)}' extends {dataEnd - bytes.Length} bytes beyond resource EOF.");
                    break;
                }

                long next = dataEnd + (size & 1);
                if (next <= offset || next > int.MaxValue)
                    break;

                offset = (int)next;
                chunkIndex++;
            }

            if (offset < bytes.Length)
                Log($"{specimen}: RIFF scan ended at byte {offset}; {bytes.Length - offset} resource bytes remain.");
            else
                Log($"{specimen}: RIFF scan consumed the resource exactly.");
        }

        private static string Printable(string value)
        {
            StringBuilder result = new(value.Length);
            foreach (char c in value)
                result.Append(char.IsControl(c) ? '?' : c);
            return result.ToString();
        }
    }
}
