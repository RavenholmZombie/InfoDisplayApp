using CefSharp;
using CefSharp.WinForms;
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

            // Keep the existing experiment switch so the current Visual Studio
            // launch profile does not need to change while we swap browser engines.
            bool runBrowserExperiment = args.Any(arg =>
                arg.Equals("--firefox-kiosk-experiment", StringComparison.OrdinalIgnoreCase));

            if (runBrowserExperiment)
            {
                string cachePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "InfoDisplayApp",
                    "CefSharpExperiment");

                CefSettings settings = new()
                {
                    CachePath = cachePath,
                    PersistSessionCookies = true
                };

                settings.CefCommandLineArgs.Add(
                    "autoplay-policy",
                    "no-user-gesture-required");

                bool initialized = Cef.Initialize(
                    settings,
                    performDependencyCheck: true,
                    browserProcessHandler: null);

                if (!initialized)
                {
                    MessageBox.Show(
                        $"CefSharp failed to initialize ({Cef.GetExitCode()}). " +
                        "Check the CefSharp log and runtime files in the output directory.",
                        "InfoDisplay CefSharp Experiment",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    frmCefSharpExperiment experimentForm = new();
                    AppMessages.Initialize(experimentForm);
                    Application.Run(experimentForm);
                }
                finally
                {
                    Cef.Shutdown();
                }

                return;
            }

            frmMain mainForm = new();
            AppMessages.Initialize(mainForm);

            Application.Run(mainForm);
        }
    }
}
