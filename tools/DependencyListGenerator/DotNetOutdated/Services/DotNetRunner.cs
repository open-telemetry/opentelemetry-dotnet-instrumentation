using System.Diagnostics;
using System.IO;
using System.Text;
using McMaster.Extensions.CommandLineUtils;

namespace DependencyListGenerator.DotNetOutdated.Services;

/// <remarks>
/// Credit for the stuff happening in here goes to the https://github.com/jaredcnance/dotnet-status project
/// </remarks>
public static class DotNetRunner
{
    public static RunStatus Run(string workingDirectory, string[] arguments)
    {
        // 1. Get the path from the library
        var dotnetPath = DotNetExe.FullPathOrDefault();

        // 2. Check if the file actually exists. If not, just use "dotnet"
        // and let the OS find it in the PATH.
        // on Linux DotNetExe.FullPathOrDefault() may wrongly return "/usr/local/share/dotnet/dotnet"
        if (dotnetPath == null || !File.Exists(dotnetPath))
        {
            dotnetPath = "dotnet";
        }

        var psi = new ProcessStartInfo(dotnetPath, string.Join(" ", arguments))
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var p = new Process();
        try
        {
            p.StartInfo = psi;
            p.Start();

            var output = new StringBuilder();
            var errors = new StringBuilder();
            var outputTask = ConsumeStreamReaderAsync(p.StandardOutput, output);
            var errorTask = ConsumeStreamReaderAsync(p.StandardError, errors);

            var processExited = p.WaitForExit(20000);

            if (processExited == false)
            {
                p.Kill();

                return new RunStatus(output.ToString(), errors.ToString(), exitCode: -1);
            }

            Task.WaitAll(outputTask, errorTask);

            return new RunStatus(output.ToString(), errors.ToString(), p.ExitCode);
        }
        finally
        {
            p.Dispose();
        }
    }

    private static async Task ConsumeStreamReaderAsync(StreamReader reader, StringBuilder lines)
    {
        await Task.Yield();

        string line;
        while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
        {
            lines.AppendLine(line);
        }
    }
}
