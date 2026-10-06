using InfoDisplayApp.Properties;

namespace InfoDisplayApp
{
    public partial class frmMain
    {
        /// <summary>
        /// Resolves the initial Internet state while frmMain is still hidden behind
        /// the startup/loading UI. This prevents an online launch from briefly
        /// presenting the Offline Mode page.
        /// </summary>
        internal async Task CheckInitialConnectivityAsync(CancellationToken cancellationToken = default)
        {
            await _connectivity.StartAsync();

            if (IsDisposed || Disposing)
                return;

            if (_connectivity.IsOnline)
                ApplyOnlineMode(restoreContent: true);
            else
                EnterOfflineMode();
        }

        /// <summary>
        /// Waits for startup-critical UI components to finish their initial work.
        /// For now the normal text ticker is the gate because its first message
        /// depends on status/weather initialization and would otherwise appear
        /// blank for a moment after the dashboard is revealed.
        /// </summary>
        internal async Task WaitForStartupReadyAsync(CancellationToken cancellationToken = default)
        {
            ctrlTicker? ticker = null;

            while (!IsDisposed && ticker == null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ticker = pnlTicker.Controls
                    .OfType<ctrlTicker>()
                    .FirstOrDefault();

                if (ticker == null)
                    await Task.Delay(25, cancellationToken);
            }

            if (ticker == null)
                throw new InvalidOperationException("The startup ticker control was not created.");

            await ticker.WaitUntilStartupReadyAsync(cancellationToken);
        }
    }
}
