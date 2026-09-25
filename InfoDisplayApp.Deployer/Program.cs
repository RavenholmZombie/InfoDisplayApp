using System.Diagnostics;

namespace InfoDisplayApp.Deployer;

internal static class Program
{
    private const string BatchFileName = "Deploy InfoScreen.bat";
    private const string PublishProfileName = "FolderProfile1";

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

            if (!File.Exists(publishProfilePath))
            {
                WriteError($"Publish profile was not found:{Environment.NewLine}{publishProfilePath}");
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
            Console.WriteLine($"Publish profile: {PublishProfileName}");
            Console.WriteLine($"Launcher:        {batchPath}");
            Console.WriteLine();

            int publishExitCode = PublishInfoScreen(projectPath, solutionRoot);

            if (publishExitCode != 0)
            {
                WriteError($"Publishing failed with exit code {publishExitCode}. Deployment aborted.");
                return publishExitCode;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine("InfoScreen published successfully.");
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

    private static int PublishInfoScreen(string projectPath, string solutionRoot)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Publishing InfoScreen...");
        Console.ResetColor();
        Console.WriteLine();

        ProcessStartInfo publishStartInfo = new()
        {
            FileName = "dotnet",
            WorkingDirectory = solutionRoot,
            UseShellExecute = false
        };

        publishStartInfo.ArgumentList.Add("publish");
        publishStartInfo.ArgumentList.Add(projectPath);
        publishStartInfo.ArgumentList.Add("-p:PublishProfile=" + PublishProfileName);

        using Process? publishProcess = Process.Start(publishStartInfo);

        if (publishProcess == null)
        {
            WriteError("Could not start dotnet publish.");
            return 1;
        }

        publishProcess.WaitForExit();
        return publishProcess.ExitCode;
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
