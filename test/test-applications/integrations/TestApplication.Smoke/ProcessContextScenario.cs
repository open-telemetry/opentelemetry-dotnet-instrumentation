// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using OpenTelemetry;

namespace TestApplication.Smoke;

internal static class ProcessContextScenario
{
    private const string ExplicitTraceParent = "00-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-bbbbbbbbbbbbbbbb-01";
    private static readonly ActivitySource Source = new("TestApplication.ProcessContext");

    internal static bool TryRun(string[] args)
    {
        if (args.Length == 0)
        {
            return false;
        }

        switch (args[0])
        {
            case "process-context-roots":
                EmitRoots();
                return true;
            case "process-context-launch":
                LaunchChildren();
                return true;
            case "process-context-child":
                WriteBaggage("startup");
                Invoke("process-child");

                return true;
            default:
                return false;
        }
    }

    private static void EmitRoots()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name is "TestApplication.ProcessContext" or "OpenTelemetry.AutoInstrumentation.NoCode",
            Sample = static (ref ActivityCreationOptions<ActivityContext> options) =>
            {
                var links = string.Join(",", (options.Links ?? []).Select(link => $"{link.Context.TraceId}:{link.Context.SpanId}"));
                Write("sample", options.Name, options.Parent.TraceId.ToString(), options.Parent.SpanId.ToString(), links, options.Parent.IsRemote ? "1" : "0");
                return ActivitySamplingResult.None;
            }
        };
        ActivitySource.AddActivityListener(listener);
        WriteBaggage("startup");
        using var delayed = Source.CreateActivity("delayed", ActivityKind.Internal);

        Invoke("invocation");
        Invoke("repeated");
        InvokeAsync("task").GetAwaiter().GetResult();
        if (InvokeTaskResultAsync("task-result").GetAwaiter().GetResult() != 42)
        {
            throw new InvalidOperationException("The configured invocation changed the task result.");
        }

#if NET
        InvokeValueTaskAsync("value-task").AsTask().GetAwaiter().GetResult();
#endif
        using (var arbitrary = Source.StartActivity("arbitrary"))
        {
            WriteActivity("arbitrary", arbitrary);
        }

        delayed?.Start();
        WriteActivity("delayed", delayed);
        delayed?.Stop();

        if (!ActivityContext.TryParse(ExplicitTraceParent, "explicit=value", isRemote: true, out var explicitContext))
        {
            throw new InvalidOperationException("The explicit trace context is invalid.");
        }

        using (var explicitParent = Source.StartActivity("explicit-context", ActivityKind.Internal, explicitContext))
        {
            WriteActivity("explicit-context", explicitParent);
            Invoke("ambient-invocation");
        }

        using (var explicitParent = Source.StartActivity("explicit-string", ActivityKind.Internal, ExplicitTraceParent))
        {
            WriteActivity("explicit-string", explicitParent);
        }

        using (var linked = Source.StartActivity("existing-links", ActivityKind.Internal, default(ActivityContext), links: [new ActivityLink(explicitContext)]))
        {
            WriteActivity("existing-links", linked);
        }

        Baggage.SetBaggage("origin", "updated");
        Invoke("baggage-updated");
        WriteBaggage("updated");
        if (Activity.Current is not null)
        {
            throw new InvalidOperationException("A configured invocation leaked its activity into the caller.");
        }

        Write("lifetime", "restored");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Invoke(string label)
    {
        WriteInvocation(label);
        if (label == "invocation")
        {
            Invoke("nested");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task InvokeAsync(string label)
    {
        await Task.Yield();
        WriteInvocation(label);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<int> InvokeTaskResultAsync(string label)
    {
        await Task.Yield();
        WriteInvocation(label);
        return 42;
    }

#if NET
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async ValueTask InvokeValueTaskAsync(string label)
    {
        await Task.Yield();
        WriteInvocation(label);
    }
#endif

    private static void WriteInvocation(string label)
    {
        if (Activity.Current?.DisplayName == label)
        {
            WriteActivity(label, Activity.Current);
        }
        else
        {
            Write("dropped", label);
        }
    }

    private static void LaunchChildren()
    {
        var originalTraceParent = Environment.GetEnvironmentVariable("TRACEPARENT");
        var originalTraceState = Environment.GetEnvironmentVariable("TRACESTATE");
        var originalBaggage = Environment.GetEnvironmentVariable("BAGGAGE");
        WriteBaggage("startup");
        Baggage.SetBaggage("origin", "updated");
        var startInfo = CreateChildStartInfo();

        using (var active = Source.StartActivity("launch-active"))
        {
            WriteActivity("launch-active", active);
            RunChild(startInfo, "active");
        }

        // A second launch uses the same ProcessStartInfo with a different current span.
        using (var reused = Source.StartActivity("launch-reused"))
        {
            WriteActivity("launch-reused", reused);
            RunChild(startInfo, "reused");
        }

        RunChild(startInfo, "fallback");

        Task.WaitAll(
            Task.Run(() => LaunchConcurrentChild(startInfo, "parallel-first")),
            Task.Run(() => LaunchConcurrentChild(startInfo, "parallel-second")));

        VerifyFailedLaunch();

        if (startInfo.EnvironmentVariables["TRACEPARENT"] != ExplicitTraceParent ||
            startInfo.EnvironmentVariables["TRACESTATE"] != "explicit=value" ||
            startInfo.EnvironmentVariables["BAGGAGE"] != "origin=explicit" ||
            startInfo.EnvironmentVariables["PROCESS_CONTEXT_CUSTOM"] != "unchanged")
        {
            throw new InvalidOperationException("Launching a child changed the application's ProcessStartInfo environment.");
        }

        if (Environment.GetEnvironmentVariable("TRACEPARENT") != originalTraceParent ||
            Environment.GetEnvironmentVariable("TRACESTATE") != originalTraceState ||
            Environment.GetEnvironmentVariable("BAGGAGE") != originalBaggage)
        {
            throw new InvalidOperationException("Launching a child changed the parent process environment.");
        }

        Write("original", "unchanged");
    }

    private static void LaunchConcurrentChild(ProcessStartInfo startInfo, string label)
    {
        using var activity = Source.StartActivity($"launch-{label}");
        WriteActivity($"launch-{label}", activity);
        RunChild(startInfo, label);
    }

    private static void VerifyFailedLaunch()
    {
        var startInfo = new ProcessStartInfo(Path.Combine(Path.GetTempPath(), $"otel-missing-{Guid.NewGuid():N}"))
        {
            UseShellExecute = false
        };
        startInfo.EnvironmentVariables["TRACEPARENT"] = "sentinel";
        startInfo.EnvironmentVariables["TRACESTATE"] = "sentinel=value";
        startInfo.EnvironmentVariables["BAGGAGE"] = "origin=sentinel";

        try
        {
            using var unexpected = Process.Start(startInfo);
            unexpected?.Kill();
            throw new InvalidOperationException("Launching a missing executable unexpectedly succeeded.");
        }
        catch (Win32Exception)
        {
            // The operating system rejected the launch after context injection ran.
        }

        if (startInfo.EnvironmentVariables["TRACEPARENT"] != "sentinel" ||
            startInfo.EnvironmentVariables["TRACESTATE"] != "sentinel=value" ||
            startInfo.EnvironmentVariables["BAGGAGE"] != "origin=sentinel")
        {
            throw new InvalidOperationException("The failed launch changed the application's ProcessStartInfo environment.");
        }

        Write("failed-launch", "unchanged");
    }

    private static ProcessStartInfo CreateChildStartInfo()
    {
        var assemblyPath = typeof(Program).Assembly.Location;
#if NETFRAMEWORK
        var startInfo = new ProcessStartInfo(assemblyPath, "process-context-child");
#else
        var startInfo = new ProcessStartInfo("dotnet");
        startInfo.ArgumentList.Add(assemblyPath);
        startInfo.ArgumentList.Add("process-context-child");
#endif
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.CreateNoWindow = true;
        startInfo.EnvironmentVariables["TRACEPARENT"] = ExplicitTraceParent;
        startInfo.EnvironmentVariables["TRACESTATE"] = "explicit=value";
        startInfo.EnvironmentVariables["BAGGAGE"] = "origin=explicit";
        startInfo.EnvironmentVariables["PROCESS_CONTEXT_CUSTOM"] = "unchanged";
        return startInfo;
    }

    private static void RunChild(ProcessStartInfo startInfo, string label)
    {
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Child process did not start.");
        if (!ReferenceEquals(startInfo, process.StartInfo))
        {
            throw new InvalidOperationException("Launching a child replaced the application's ProcessStartInfo.");
        }

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

        foreach (var line in output.Result.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            Console.WriteLine($"PROCESS_CHILD|{label}|{line}");
        }
    }

    private static void WriteBaggage(string label)
    {
        Write("baggage", label, Baggage.Current.GetBaggage("origin") ?? string.Empty);
    }

    private static void WriteActivity(string label, Activity? activity)
    {
        if (activity == null)
        {
            throw new InvalidOperationException($"Activity '{label}' was not created.");
        }

        var links = string.Join(",", activity.Links.Select(link => $"{link.Context.TraceId}:{link.Context.SpanId}"));
        Write("activity", label, activity.TraceId.ToString(), activity.SpanId.ToString(), activity.ParentSpanId.ToString(), activity.Recorded ? "1" : "0", activity.TraceStateString ?? string.Empty, links, activity.HasRemoteParent ? "1" : "0", activity.Source.Name);
    }

    private static void Write(params string[] values)
    {
        Console.WriteLine($"PROCESS_CONTEXT|{string.Join("|", values)}");
    }
}
