// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace TestApplication.CustomSdk;

internal static class ProcessContextScenario
{
    private const string SourceName = "TestApplication.CustomSdk.ProcessContext";

    internal static bool TryRun(string[] args)
    {
        if (args.Length != 1 || args[0] is not ("--process-context" or "--process-context-child"))
        {
            return false;
        }

        // The agent has already initialized with SDK setup disabled. Building the
        // application's SDK here must not be required for its default propagators.
        using var provider = Sdk.CreateTracerProviderBuilder()
            .AddSource(SourceName)
            .AddOtlpExporter()
            .Build();
        using var source = new ActivitySource(SourceName);
        var name = args[0] == "--process-context-child" ? "child" : "root";
        using var activity = source.StartActivity(name) ?? throw new InvalidOperationException("Activity was not created.");
        Console.WriteLine($"PROCESS_CONTEXT_CUSTOM_SDK|{name}|{activity.TraceId}|{activity.SpanId}|{activity.ParentSpanId}|{(activity.HasRemoteParent ? "1" : "0")}|{(activity.Recorded ? "1" : "0")}|{activity.TraceStateString}|{Baggage.Current.GetBaggage("origin")}");

        if (name == "root")
        {
            Baggage.SetBaggage("origin", "updated");
            RunChild();
        }

        return true;
    }

    private static void RunChild()
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add(typeof(Program).Assembly.Location);
        startInfo.ArgumentList.Add("--process-context-child");
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Child process did not start.");
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(20_000))
        {
            process.Kill();
            throw new TimeoutException("Child process did not exit.");
        }

        Task.WaitAll(output, errors);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Child failed with exit code {process.ExitCode}: {errors.Result}");
        }

        Console.Write(output.Result);
    }
}
