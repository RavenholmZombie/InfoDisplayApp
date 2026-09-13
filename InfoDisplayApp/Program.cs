using CefSharp;
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
                // InfoDisplay currently builds AnyCPU. CefSharp ships separate
                // x86/x64 native runtimes, so install its architecture resolver
                // before touching anything that can load CefSharp.Core.Runtime.
                CefRuntime.SubscribeAnyCpuAssemblyResolver();

                string cachePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "InfoDisplayApp",
                    "CefSharpExperiment");

                CefSettings settings = new()
                {
                    CachePath = cachePath,
                    PersistSessionCookies = true,
                    PersistUserPreferences = true
                };

                // Streaming sites commonly expect autoplay behavior closer to a
                // living-room browser than a freshly-created embedded control.
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
                    CefRuntime.UnsubscribeAnyCpuAssemblyResolver();
                }

                return;
            }

            frmMain mainForm = new();
            AppMessages.Initialize(mainForm);

            Application.Run(mainForm);
        }
    }
}
