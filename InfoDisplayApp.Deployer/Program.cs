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

            string projectPath = Path.Combine(
                solutionRoot,
                "InfoDisplayApp",
                "InfoDisplayApp.csproj");

            string publishProfilePath = Path.Combine(
                solutionRoot,
                "InfoDisplayApp",
                "Properties",
                "PublishProfiles",
                $"{PublishProfileName}.pubxml");

            string batchPath = Path.Combine(
                solutionRoot,
                "InfoDisplayApp",
                "Build",
                BatchFileName);

            if (!File.Exists(projectPath))
            {
                WriteError($"InfoScreen project was not found:{Environment.NewLine}{projectPath}");
                return 1;
            }

            if (!File.Exists(batchPath))
            {
                WriteError($"Deployment launcher was not found:{Environment.NewLine}{batchPath}");
                return 1;
            }

            Console.WriteLine("==========================================");
            Console.WriteLine("         InfoScreen Deployer");
            Console.WriteLine("==========================================");
            Console.WriteLine();
            Console.WriteLine($"Project:         {projectPath}");
            Console.WriteLine($"Launcher:        {batchPath}");
            Console.WriteLine();

            int buildExitCode = BuildInfoScreen(projectPath, solutionRoot);

            if (buildExitCode != 0)
            {
                WriteError($"Release build failed with exit code {buildExitCode}. Deployment aborted.");
                return buildExitCode;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine("InfoScreen Release build completed successfully.");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Handing deployment over to the existing launcher...");
            Console.WriteLine();

            ProcessStartInfo deployStartInfo = new()
            {
                FileName = batchPath,
                WorkingDirectory = Path.GetDirectoryName(batchPath)!,
                UseShellExecute = true
            };

            using Process? deployProcess = Process.Start(deployStartInfo);

            if (deployProcess == null)
            {
                WriteError("Windows did not start the deployment launcher.");
                return 1;
            }

            deployProcess.WaitForExit();
            return deployProcess.ExitCode;
        }
        catch (Exception ex)
        {
            WriteError(ex.ToString());
            return 1;
        }
    }

    private static int BuildInfoScreen(string projectPath, string solutionRoot)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Building InfoScreen (Release)...");
        Console.ResetColor();
        Console.WriteLine();

        ProcessStartInfo buildStartInfo = new()
        {
            FileName = "dotnet",
            WorkingDirectory = solutionRoot,
            UseShellExecute = false
        };

        buildStartInfo.ArgumentList.Add("build");
        buildStartInfo.ArgumentList.Add(projectPath);
        buildStartInfo.ArgumentList.Add("-c");
        buildStartInfo.ArgumentList.Add("Release");

        using Process? buildProcess = Process.Start(buildStartInfo);

        if (buildProcess == null)
        {
            WriteError("Could not start dotnet build.");
            return 1;
        }

        buildProcess.WaitForExit();
        return buildProcess.ExitCode;
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
