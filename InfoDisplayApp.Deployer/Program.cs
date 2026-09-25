using System.Diagnostics;

namespace InfoDisplayApp.Deployer;

internal static class Program
{
    private const string BatchFileName = "Deploy InfoScreen.bat";

    private static int Main()
    {
        Console.Title = "InfoScreen Deployer";

        try
        {
            string? solutionRoot = FindSolutionRoot();

            if (solutionRoot == null)
            {
                WriteError("Could not locate InfoDisplayApp.slnx.");
                return 1;
            }

            string batchPath = Path.Combine(
                solutionRoot,
                "InfoDisplayApp",
                "Build",
                BatchFileName);

            if (!File.Exists(batchPath))
            {
                WriteError($"Deployment launcher was not found:{Environment.NewLine}{batchPath}");
                return 1;
            }

            Console.WriteLine("==========================================");
            Console.WriteLine("         InfoScreen Deployer");
            Console.WriteLine("==========================================");
            Console.WriteLine();
            Console.WriteLine($"Launcher: {batchPath}");
            Console.WriteLine();
            Console.WriteLine("Handing deployment over to the existing launcher...");
            Console.WriteLine();

            ProcessStartInfo startInfo = new()
            {
                FileName = batchPath,
                WorkingDirectory = Path.GetDirectoryName(batchPath)!,
                UseShellExecute = true
            };

            using Process? process = Process.Start(startInfo);

            if (process == null)
            {
                WriteError("Windows did not start the deployment launcher.");
                return 1;
            }

            process.WaitForExit();
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            WriteError(ex.ToString());
            return 1;
        }
    }

    private static string? FindSolutionRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "InfoDisplayApp.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        // When Visual Studio launches this project from an unusual output
        // directory, the current working directory is another useful anchor.
        directory = new DirectoryInfo(Environment.CurrentDirectory);

        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "InfoDisplayApp.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        return null;
    }

    private static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("ERROR: " + message);
        Console.ResetColor();
    }
}
