// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using System.Diagnostics;
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
                using (var child = Source.StartActivity("process-child"))
                {
                    WriteActivity("process-child", child);
                }

                return true;
            default:
                return false;
        }
    }

    private static void EmitRoots()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == "TestApplication.ProcessContext",
            Sample = static (ref ActivityCreationOptions<ActivityContext> options) =>
            {
                var links = string.Join(",", (options.Links ?? []).Select(link => $"{link.Context.TraceId}:{link.Context.SpanId}"));
                Write("sample", options.Name, options.Parent.TraceId.ToString(), options.Parent.SpanId.ToString(), links, options.Parent.IsRemote ? "1" : "0");
                return ActivitySamplingResult.None;
            }
        };
        ActivitySource.AddActivityListener(listener);
        WriteBaggage("startup");
        VerifySkippedActivity(registerDroppingListener: false);
        VerifySkippedActivity(registerDroppingListener: true);
        using var delayed = Source.CreateActivity("delayed", ActivityKind.Internal);

        using (var root = Source.StartActivity("root"))
        {
            WriteActivity("root", root);
            using var child = Source.StartActivity("child");
            if (child is null && root is { Recorded: false })
            {
                // The SDK may omit local children of an unsampled propagation-only activity.
                Write("dropped", "child");
            }
            else
            {
                WriteActivity("child", child);
            }
        }

        // Later environment changes must not replace the context cached at startup.
        var startupTraceParent = Environment.GetEnvironmentVariable("TRACEPARENT");
        try
        {
            Environment.SetEnvironmentVariable("TRACEPARENT", ExplicitTraceParent);
            using var repeated = Source.StartActivity("repeated");
            WriteActivity("repeated", repeated);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TRACEPARENT", startupTraceParent);
        }

        using (var kindFirst = Source.StartActivity(ActivityKind.Internal, name: "kind-first"))
        {
            WriteActivity("kind-first", kindFirst);
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
            using var ambient = Source.StartActivity("ambient");
            WriteActivity("ambient", ambient);
        }

        using (var explicitParent = Source.StartActivity("explicit-string", ActivityKind.Internal, ExplicitTraceParent))
        {
            WriteActivity("explicit-string", explicitParent);
        }

        // Empty string parent IDs also select a root. Force W3C format so the
        // normal ActivityContext sampling callback handles this empty string.
        var forceDefaultIdFormat = Activity.ForceDefaultIdFormat;
        try
        {
            Activity.ForceDefaultIdFormat = true;
            using var explicitRoot = Source.StartActivity("explicit-root", ActivityKind.Internal, parentId: string.Empty);
            WriteActivity("explicit-root", explicitRoot);
        }
        finally
        {
            Activity.ForceDefaultIdFormat = forceDefaultIdFormat;
        }

        var rootContext = new ActivityContext(ActivityTraceId.CreateFromString("cccccccccccccccccccccccccccccccc".AsSpan()), default, ActivityTraceFlags.Recorded);
        using (var explicitRoot = Source.StartActivity("explicit-root-context", ActivityKind.Internal, rootContext))
        {
            WriteActivity("explicit-root-context", explicitRoot);
        }

        using (var linked = Source.StartActivity("existing-links", ActivityKind.Internal, default(ActivityContext), links: [new ActivityLink(explicitContext)]))
        {
            WriteActivity("existing-links", linked);
        }

        if (ActivityContext.TryParse(startupTraceParent, Environment.GetEnvironmentVariable("TRACESTATE"), isRemote: true, out var incomingContext))
        {
            using var linked = Source.StartActivity("already-linked", ActivityKind.Internal, default(ActivityContext), links: [new ActivityLink(incomingContext)]);
            WriteActivity("already-linked", linked);
        }

        EmitAsyncRoot().GetAwaiter().GetResult();
        Baggage.SetBaggage("origin", "updated");
        using (var updated = Source.StartActivity("baggage-updated"))
        {
            WriteActivity("baggage-updated", updated);
        }

        WriteBaggage("updated");
        if (Activity.Current is not null)
        {
            throw new InvalidOperationException("Process context extraction leaked an activity into the caller.");
        }

        Write("lifetime", "restored");
    }

    private static async Task EmitAsyncRoot()
    {
        await Task.Yield();
        using var activity = Source.StartActivity("task");
        WriteActivity("task", activity);
    }

    private static void VerifySkippedActivity(bool registerDroppingListener)
    {
        var previousActivity = Activity.Current;
        var linksEnumerated = false;
        var samplingAttempts = 0;
        var label = registerDroppingListener ? "dropping-listener" : "unsubscribed";
        using var source = new ActivitySource($"TestApplication.ProcessContext.{label}");
        using var listener = new ActivityListener
        {
            ShouldListenTo = candidate => registerDroppingListener && ReferenceEquals(candidate, source),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
            {
                samplingAttempts++;
                return ActivitySamplingResult.None;
            }
        };
        ActivitySource.AddActivityListener(listener);
        using var activity = source.StartActivity(label, ActivityKind.Internal, default(ActivityContext), links: UnexpectedLinks());
        if (activity is not null || linksEnumerated || !ReferenceEquals(previousActivity, Activity.Current) ||
            samplingAttempts != (registerDroppingListener ? 1 : 0))
        {
            throw new InvalidOperationException("Skipping activity creation changed its context, enumerated links, or sampled an unexpected number of times.");
        }

        Write(label, "unchanged");

        IEnumerable<ActivityLink> UnexpectedLinks()
        {
            linksEnumerated = true;
            yield return new ActivityLink(default);
            throw new InvalidOperationException("Links for an activity that is not created must not be enumerated.");
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
