using System.Diagnostics;
using System.Text;
using McMaster.Extensions.CommandLineUtils;

namespace DependencyListGenerator.DotNetOutdated.Services;

/// <remarks>
/// Credit for the stuff happening in here goes to the https://github.com/jaredcnance/dotnet-status project
/// </remarks>
public static class DotNetRunner
{
    private const int CommandTimeoutMilliseconds = 20_000;
    private const int OutputDrainTimeoutMilliseconds = 5_000;

    public static RunStatus Run(string workingDirectory, string[] arguments)
    {
        // 1. Get the path from the library
        var dotnetPath = DotNetExe.FullPathOrDefault();

        // 2. Check if the file actually exists. If not, just use "dotnet"
        // and let the OS find it in the PATH.
        // on Linux DotNetExe.FullPathOrDefault() may wrongly return "/usr/local/share/dotnet/dotnet"
        // https://github.com/natemcmaster/CommandLineUtils/issues/600
        if (!File.Exists(dotnetPath))
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
        psi.Environment["MSBUILDDISABLENODEREUSE"] = "1";

        var p = new Process();
        using var outputClosed = new ManualResetEventSlim();
        using var errorClosed = new ManualResetEventSlim();

        try
        {
            p.StartInfo = psi;

            var output = new StringBuilder();
            var errors = new StringBuilder();

            p.OutputDataReceived += (_, args) => AppendLineOrClose(output, args.Data, outputClosed);
            p.ErrorDataReceived += (_, args) => AppendLineOrClose(errors, args.Data, errorClosed);

            p.Start();

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            var processExited = p.WaitForExit(CommandTimeoutMilliseconds);

            if (processExited == false)
            {
                KillProcessTree(p);
                WaitForOutputToDrain(outputClosed, errorClosed, p);

                return new RunStatus(GetOutput(output), GetOutput(errors), exitCode: -1);
            }

            WaitForOutputToDrain(outputClosed, errorClosed, p);

            return new RunStatus(GetOutput(output), GetOutput(errors), p.ExitCode);
        }
        finally
        {
            p.Dispose();
        }
    }

    private static void AppendLineOrClose(StringBuilder lines, string line, ManualResetEventSlim closed)
    {
        if (line is null)
        {
            closed.Set();
            return;
        }

        lock (lines)
        {
            lines.AppendLine(line);
        }
    }

    private static string GetOutput(StringBuilder lines)
    {
        lock (lines)
        {
            return lines.ToString();
        }
    }

    private static void WaitForOutputToDrain(ManualResetEventSlim outputClosed, ManualResetEventSlim errorClosed, Process process)
    {
        if (!outputClosed.Wait(OutputDrainTimeoutMilliseconds))
        {
            CancelOutputRead(process);
        }

        if (!errorClosed.Wait(OutputDrainTimeoutMilliseconds))
        {
            CancelErrorRead(process);
        }
    }

    private static void KillProcessTree(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // The process exited between WaitForExit and Kill.
        }
    }

    private static void CancelOutputRead(Process process)
    {
        try
        {
            process.CancelOutputRead();
        }
        catch (InvalidOperationException)
        {
            // The stream already closed.
        }
    }

    private static void CancelErrorRead(Process process)
    {
        try
        {
            process.CancelErrorRead();
        }
        catch (InvalidOperationException)
        {
            // The stream already closed.
        }
    }
}
