// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;

namespace TestApplication.ContinuousProfiler;

internal static class RuntimeReconfigurationScenario
{
    private const uint InitialInterval = 1000u;
    private const uint ActiveInterval = 5000u;
    private const uint IntermediateInterval = 2000u;
    private const uint ReconfiguredInterval = 1000u;

    public static void Run()
    {
        Thread.Sleep(TimeSpan.FromMilliseconds(InitialInterval + 500u));
        Ensure(
            RuntimeReconfigurationPlugin.GetThreadExportCount() == 0,
            "A CPU sample was exported while thread sampling was initially disabled.");

        try
        {
            Ensure(
                !RuntimeContinuousProfilerNativeMethods.ConfigureContinuousProfiler(
                    threadSamplingEnabled: true,
                    threadSamplingInterval: 0,
                    allocationSamplingEnabled: false,
                    maxMemorySamplesPerMinute: 0,
                    selectedThreadSamplingInterval: 0),
                "ConfigureContinuousProfiler reported success for an invalid sampling interval.");

            Ensure(
                RuntimeContinuousProfilerNativeMethods.ConfigureContinuousProfiler(
                    threadSamplingEnabled: true,
                    threadSamplingInterval: ActiveInterval,
                    allocationSamplingEnabled: false,
                    maxMemorySamplesPerMinute: 0,
                    selectedThreadSamplingInterval: 0),
                "ConfigureContinuousProfiler failed to enable CPU sampling.");

            Thread.Sleep(TimeSpan.FromSeconds(2));
            Ensure(
                RuntimeReconfigurationPlugin.GetThreadExportCount() == 0,
                "Repeated ConfigureContinuousProfiler did not apply the new sampling interval.");

            var exportCount = WaitForExportAfter(0, TimeSpan.FromSeconds(4));
            Ensure(exportCount > 0, "Repeated ConfigureContinuousProfiler did not enable CPU sampling.");
            Ensure(
                RuntimeReconfigurationPlugin.GetLastThreadSamplingInterval() == ActiveInterval,
                "The exporter did not receive the active sampling interval.");

            Ensure(
                !RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerSamplingInterval(0),
                "SetContinuousProfilerSamplingInterval reported success for an invalid sampling interval.");

            // Rapid accepted updates may be coalesced, but the sampler must eventually use the latest one.
            Ensure(
                RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerSamplingInterval(IntermediateInterval),
                "SetContinuousProfilerSamplingInterval failed for the intermediate interval.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerSamplingInterval(ReconfiguredInterval),
                "SetContinuousProfilerSamplingInterval failed for the final interval.");
            var exportCountAfterIntervalChange = WaitForSamplingInterval(
                ReconfiguredInterval,
                exportCount,
                TimeSpan.FromSeconds(3));
            Ensure(
                exportCountAfterIntervalChange > exportCount,
                "The sampler did not observe the reconfigured sampling interval.");

            Ensure(
                RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerEnabled(false),
                "SetContinuousProfilerEnabled failed to disable CPU sampling.");
            var stoppedExportCount = WaitForExporterIdle(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(3));
            Ensure(stoppedExportCount >= 0, "The CPU exporter did not become idle after thread sampling was disabled.");

            Thread.Sleep(TimeSpan.FromMilliseconds(ReconfiguredInterval + 500u));
            Ensure(
                RuntimeReconfigurationPlugin.GetThreadExportCount() == stoppedExportCount,
                "A CPU sample was exported while thread sampling was disabled.");

            Ensure(
                RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerEnabled(true),
                "SetContinuousProfilerEnabled failed to enable CPU sampling.");
            Ensure(
                WaitForExportAfter(stoppedExportCount, TimeSpan.FromSeconds(3)) > stoppedExportCount,
                "No CPU sample was exported after thread sampling was re-enabled.");

            Console.WriteLine("runtime-reconfiguration-completed");
        }
        finally
        {
            _ = RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerEnabled(false);
        }
    }

    private static int WaitForExportAfter(int previousExportCount, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            var exportCount = RuntimeReconfigurationPlugin.GetThreadExportCount();
            if (exportCount > previousExportCount)
            {
                return exportCount;
            }

            Thread.Sleep(50);
        }

        return RuntimeReconfigurationPlugin.GetThreadExportCount();
    }

    private static int WaitForSamplingInterval(uint samplingInterval, int previousExportCount, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            var exportCount = RuntimeReconfigurationPlugin.GetThreadExportCount();
            if (exportCount > previousExportCount &&
                RuntimeReconfigurationPlugin.GetLastThreadSamplingInterval() == samplingInterval)
            {
                return exportCount;
            }

            // A capture already in progress, or a completed batch already queued for export,
            // may still use the previous interval after the setter accepts the change.
            Thread.Sleep(50);
        }

        return RuntimeReconfigurationPlugin.GetLastThreadSamplingInterval() == samplingInterval
            ? RuntimeReconfigurationPlugin.GetThreadExportCount()
            : previousExportCount;
    }

    private static int WaitForExporterIdle(TimeSpan quietPeriod, TimeSpan timeout)
    {
        var timeoutStopwatch = Stopwatch.StartNew();
        var quietStopwatch = Stopwatch.StartNew();
        var lastExportCount = RuntimeReconfigurationPlugin.GetThreadExportCount();

        while (timeoutStopwatch.Elapsed < timeout)
        {
            Thread.Sleep(50);
            var exportCount = RuntimeReconfigurationPlugin.GetThreadExportCount();
            if (exportCount != lastExportCount)
            {
                lastExportCount = exportCount;
                quietStopwatch.Restart();
                continue;
            }

            if (quietStopwatch.Elapsed >= quietPeriod)
            {
                return exportCount;
            }
        }

        return -1;
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
