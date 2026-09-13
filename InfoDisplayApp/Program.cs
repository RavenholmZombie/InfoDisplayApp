using InfoDisplayApp.Experiments;

namespace InfoDisplayApp
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            bool runFirefoxExperiment = args.Any(arg =>
                arg.Equals("--firefox-kiosk-experiment", StringComparison.OrdinalIgnoreCase));

            if (runFirefoxExperiment)
            {
                frmFirefoxKioskExperiment experimentForm = new();
                AppMessages.Initialize(experimentForm);
                Application.Run(experimentForm);
                return;
            }

            frmMain mainForm = new();
            AppMessages.Initialize(mainForm);

            Application.Run(mainForm);
        }
    }
}
