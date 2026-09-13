using System.Diagnostics;

namespace InfoDisplayApp.Properties
{
    public partial class ctrlTicker
    {
        /// <summary>
        /// Waits until the normal ticker has resolved its startup data, rendered
        /// its first non-empty message, and started the scrolling animation.
        /// This lets the splash screen remain visible until the ticker is truly
        /// ready instead of revealing a temporarily blank ticker panel.
        /// </summary>
        internal async Task WaitUntilStartupReadyAsync(CancellationToken cancellationToken = default)
        {
            while (!IsDisposed)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_animationRunning &&
                    !string.IsNullOrWhiteSpace(_renderedMessage) &&
                    _messages.Count > 0)
                {
                    Debug.WriteLine("Ticker: startup text is rendered and animation is ready.");
                    return;
                }

                await Task.Delay(50, cancellationToken);
            }

            throw new ObjectDisposedException(nameof(ctrlTicker));
        }
    }
}
