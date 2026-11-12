// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using System.Diagnostics;

namespace IntegrationTests.Helpers;

internal static class AdditionalDepsHelper
{
    private static readonly Lazy<string> GeneratedDirectory = new(() =>
    {
        var outputDirectory = Path.Combine(
            EnvironmentTools.GetSolutionDirectory(),
            "test-artifacts",
            "additional-deps",
            Guid.NewGuid().ToString("N"));

        Generate(outputDirectory);
        return outputDirectory;
    });

    public static string OutputDirectory => GeneratedDirectory.Value;

    public static void Generate(string outputDirectory, ITestOutputHelper? output = null)
    {
        var tracerHome = EnvironmentHelper.GetNukeBuildOutput();
        var startInfo = new ProcessStartInfo
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // The same target-framework test assembly runs on Windows and Unix, so host selection must be
        // made at runtime rather than with target-framework compilation symbols.
        if (EnvironmentTools.IsWindows())
        {
            startInfo.FileName = "powershell.exe";
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-ExecutionPolicy");
            startInfo.ArgumentList.Add("Bypass");
            startInfo.ArgumentList.Add("-File");
            startInfo.ArgumentList.Add(Path.Combine(tracerHome, "generate-additional-deps.ps1"));
            startInfo.ArgumentList.Add("-OutputPath");
            startInfo.ArgumentList.Add(outputDirectory);
        }
        else
        {
            startInfo.FileName = "/bin/sh";
            startInfo.ArgumentList.Add(Path.Combine(tracerHome, "generate-additional-deps.sh"));
            startInfo.ArgumentList.Add("--output");
            startInfo.ArgumentList.Add(outputDirectory);
        }

        using var process = Process.Start(startInfo);
        Assert.NotNull(process);
        using var helper = new ProcessHelper(process);

        var exitedInTime = process!.WaitForExit((int)TestTimeout.ProcessExit.TotalMilliseconds);
        if (!exitedInTime)
        {
            process.Kill();
            process.WaitForExit();
        }

        if (output != null)
        {
            output.WriteLine($"AdditionalDeps generator exit code: {process.ExitCode}");
            output.WriteResult(helper);
        }

        Assert.True(exitedInTime, "AdditionalDeps generator timed out");
        Assert.True(process.ExitCode == 0, $"AdditionalDeps generator exited with code {process.ExitCode}.{Environment.NewLine}{helper.StandardOutput}{helper.ErrorOutput}");
    }
}
#endif
