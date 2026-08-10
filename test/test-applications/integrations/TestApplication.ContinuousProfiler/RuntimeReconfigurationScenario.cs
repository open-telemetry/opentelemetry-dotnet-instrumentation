// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;

namespace TestApplication.ContinuousProfiler;

internal static class RuntimeReconfigurationScenario
{
    private const uint InitialInterval = 1000u;
    private const uint ActiveInterval = 2000u;
    private const uint IntermediateInterval = 1500u;
    private const uint ReconfiguredInterval = 1000u;
    private const uint UnpreparedInterval = 50u;
    private const int ThreadSamplesBufferSize = 200 * 1024;
#if NET
    private const uint AllocationRate = 60000u;
    private const uint ReconfiguredAllocationRate = 30000u;
#endif

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
                    threadSamplingExportPipelinePrepared: false,
                    allocationSamplingEnabled: false,
                    maxMemorySamplesPerMinute: 0,
                    allocationSamplingExportPipelinePrepared: false,
                    selectedThreadSamplingInterval: 0),
                "ConfigureContinuousProfiler reported success for an invalid sampling interval.");

            Ensure(
                RuntimeContinuousProfilerNativeMethods.ConfigureContinuousProfiler(
                    threadSamplingEnabled: true,
                    threadSamplingInterval: ActiveInterval,
                    threadSamplingExportPipelinePrepared: false,
                    allocationSamplingEnabled: false,
                    maxMemorySamplesPerMinute: 0,
                    allocationSamplingExportPipelinePrepared: false,
                    selectedThreadSamplingInterval: 0),
                "ConfigureContinuousProfiler failed to enable CPU sampling.");

            Thread.Sleep(TimeSpan.FromMilliseconds(750));
            Ensure(
                RuntimeReconfigurationPlugin.GetThreadExportCount() == 0,
                "Repeated ConfigureContinuousProfiler did not apply the new sampling interval.");

            var exportCount = WaitForExportAfter(0, TimeSpan.FromSeconds(3));
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
                TimeSpan.FromSeconds(5));
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

#if NET
            Ensure(
                RuntimeContinuousProfilerNativeMethods.ConfigureContinuousProfiler(
                    threadSamplingEnabled: false,
                    threadSamplingInterval: ReconfiguredInterval,
                    threadSamplingExportPipelinePrepared: false,
                    allocationSamplingEnabled: true,
                    maxMemorySamplesPerMinute: AllocationRate,
                    allocationSamplingExportPipelinePrepared: false,
                    selectedThreadSamplingInterval: 0),
                "ConfigureContinuousProfiler failed to enable allocation sampling.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.GetContinuousProfilerAllocationSamplingRate() == AllocationRate,
                "The allocation sampler did not apply the configured sampling rate.");
            var allocationExportCount = WaitForAllocationExportAfter(0, TimeSpan.FromSeconds(5));
            Ensure(allocationExportCount > 0, "No allocation sample was exported after allocation sampling was enabled.");

            Ensure(
                RuntimeContinuousProfilerNativeMethods.ConfigureContinuousProfiler(
                    threadSamplingEnabled: false,
                    threadSamplingInterval: ReconfiguredInterval,
                    threadSamplingExportPipelinePrepared: false,
                    allocationSamplingEnabled: true,
                    maxMemorySamplesPerMinute: ReconfiguredAllocationRate,
                    allocationSamplingExportPipelinePrepared: false,
                    selectedThreadSamplingInterval: 0),
                "ConfigureContinuousProfiler failed to update the allocation sampling rate.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.GetContinuousProfilerAllocationSamplingRate() ==
                    ReconfiguredAllocationRate,
                "The allocation sampler did not apply the reconfigured sampling rate.");

            Ensure(
                RuntimeContinuousProfilerNativeMethods.ConfigureContinuousProfiler(
                    threadSamplingEnabled: false,
                    threadSamplingInterval: ReconfiguredInterval,
                    threadSamplingExportPipelinePrepared: false,
                    allocationSamplingEnabled: false,
                    maxMemorySamplesPerMinute: ReconfiguredAllocationRate,
                    allocationSamplingExportPipelinePrepared: false,
                    selectedThreadSamplingInterval: 0),
                "ConfigureContinuousProfiler failed to disable allocation sampling.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.GetContinuousProfilerAllocationSamplingRate() == 0,
                "The allocation sampler still reports an active rate after it was disabled.");
            var stoppedAllocationExportCount =
                WaitForAllocationExporterIdle(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(3));
            Ensure(stoppedAllocationExportCount >= 0, "The allocation exporter did not become idle after sampling was disabled.");

            GenerateAllocations();
            Thread.Sleep(TimeSpan.FromMilliseconds(500));
            Ensure(
                RuntimeReconfigurationPlugin.GetAllocationExportCount() == stoppedAllocationExportCount,
                "An allocation sample was exported while allocation sampling was disabled.");

            Ensure(
                RuntimeContinuousProfilerNativeMethods.ConfigureContinuousProfiler(
                    threadSamplingEnabled: false,
                    threadSamplingInterval: ReconfiguredInterval,
                    threadSamplingExportPipelinePrepared: false,
                    allocationSamplingEnabled: true,
                    maxMemorySamplesPerMinute: AllocationRate,
                    allocationSamplingExportPipelinePrepared: false,
                    selectedThreadSamplingInterval: 0),
                "ConfigureContinuousProfiler failed to re-enable allocation sampling.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.GetContinuousProfilerAllocationSamplingRate() == AllocationRate,
                "The allocation sampler did not restore the configured sampling rate.");
            Ensure(
                WaitForAllocationExportAfter(stoppedAllocationExportCount, TimeSpan.FromSeconds(5)) >
                    stoppedAllocationExportCount,
                "No allocation sample was exported after allocation sampling was re-enabled.");
#endif

            Ensure(
                RuntimeContinuousProfilerNativeMethods.ShutdownContinuousProfiler(),
                "ShutdownContinuousProfiler failed to stop continuous profiling.");
            Ensure(
                !RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerEnabled(true),
                "CPU sampling was re-enabled after continuous profiling was shut down.");
#if NET
            Ensure(
                RuntimeContinuousProfilerNativeMethods.GetContinuousProfilerAllocationSamplingRate() == 0,
                "Allocation sampling remained enabled after continuous profiling was shut down.");
#endif
            Ensure(
                RuntimeContinuousProfilerNativeMethods.ShutdownContinuousProfiler(),
                "ShutdownContinuousProfiler was not idempotent.");

            Console.WriteLine("runtime-reconfiguration-completed");
        }
        finally
        {
            _ = RuntimeContinuousProfilerNativeMethods.ShutdownContinuousProfiler();
        }
    }

    public static void VerifyUnpreparedThreadSamplingIsRejected()
    {
        try
        {
            // Initialize the native profiler without preparing either managed export pipeline.
            // A non-zero, disabled allocation rate makes this an initialization request without
            // starting allocation sampling, including on .NET Framework.
            Ensure(
                RuntimeContinuousProfilerNativeMethods.ConfigureContinuousProfiler(
                    threadSamplingEnabled: false,
                    threadSamplingInterval: 0,
                    threadSamplingExportPipelinePrepared: false,
                    allocationSamplingEnabled: false,
                    maxMemorySamplesPerMinute: 1,
                    allocationSamplingExportPipelinePrepared: false,
                    selectedThreadSamplingInterval: 0),
                "ConfigureContinuousProfiler failed to initialize the native profiler.");

            Ensure(
                !RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerSamplingInterval(UnpreparedInterval),
                "SetContinuousProfilerSamplingInterval configured an unprepared CPU export pipeline.");
            Ensure(
                !RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerEnabled(true),
                "SetContinuousProfilerEnabled enabled sampling without a prepared CPU export pipeline.");

            Thread.Sleep(TimeSpan.FromMilliseconds(UnpreparedInterval * 5u));
            var buffer = new byte[ThreadSamplesBufferSize];
            var read = RuntimeContinuousProfilerNativeMethods.ContinuousProfilerReadThreadSamples(
                buffer.Length,
                buffer,
                out var samplingInterval);
            Ensure(read == 0, "A CPU sample was captured without a prepared export pipeline.");
            Ensure(samplingInterval == 0, "An unprepared CPU sampling interval was published.");

            Console.WriteLine("runtime-unprepared-thread-rejected");
        }
        finally
        {
            _ = RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerEnabled(false);
        }
    }

#if NET
    public static void VerifyUnpreparedAllocationIsRejected()
    {
        try
        {
            Ensure(
                !RuntimeContinuousProfilerNativeMethods.ConfigureContinuousProfiler(
                    threadSamplingEnabled: false,
                    threadSamplingInterval: 0,
                    threadSamplingExportPipelinePrepared: false,
                    allocationSamplingEnabled: true,
                    maxMemorySamplesPerMinute: AllocationRate,
                    allocationSamplingExportPipelinePrepared: false,
                    selectedThreadSamplingInterval: 0),
                "ConfigureContinuousProfiler enabled allocation sampling without a prepared export pipeline.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.GetContinuousProfilerAllocationSamplingRate() == 0,
                "The allocation sampler started without a prepared export pipeline.");

            Console.WriteLine("runtime-unprepared-allocation-rejected");
        }
        finally
        {
            _ = RuntimeContinuousProfilerNativeMethods.ConfigureContinuousProfiler(
                threadSamplingEnabled: false,
                threadSamplingInterval: 0,
                threadSamplingExportPipelinePrepared: false,
                allocationSamplingEnabled: false,
                maxMemorySamplesPerMinute: 0,
                allocationSamplingExportPipelinePrepared: false,
                selectedThreadSamplingInterval: 0);
        }
    }
#endif

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

#if NET
    private static int WaitForAllocationExportAfter(int previousExportCount, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            GenerateAllocations();
            var exportCount = RuntimeReconfigurationPlugin.GetAllocationExportCount();
            if (exportCount > previousExportCount)
            {
                return exportCount;
            }

            Thread.Sleep(50);
        }

        return RuntimeReconfigurationPlugin.GetAllocationExportCount();
    }

    private static int WaitForAllocationExporterIdle(TimeSpan quietPeriod, TimeSpan timeout)
    {
        var timeoutStopwatch = Stopwatch.StartNew();
        var quietStopwatch = Stopwatch.StartNew();
        var lastExportCount = RuntimeReconfigurationPlugin.GetAllocationExportCount();

        while (timeoutStopwatch.Elapsed < timeout)
        {
            Thread.Sleep(50);
            var exportCount = RuntimeReconfigurationPlugin.GetAllocationExportCount();
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

    private static void GenerateAllocations()
    {
        for (var i = 0; i < 128; i++)
        {
            var allocation = new byte[128 * 1024];
            allocation[0] = (byte)i;
            GC.KeepAlive(allocation);
        }
    }
#endif

    private static void Ensure(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
