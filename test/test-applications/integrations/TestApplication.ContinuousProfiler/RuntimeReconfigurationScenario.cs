// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ContinuousProfiler;

internal static class RuntimeReconfigurationScenario
{
    private const uint InitialInterval = 1000u;
    private const uint ActiveInterval = 1200u;
    private const uint InitialSnapshotInterval = 100u;
    private const uint ActiveSnapshotInterval = 200u;
    private const uint UnpreparedInterval = 50u;
    private const int ThreadSamplesBufferSize = 200 * 1024;
#if NET
    private const uint InitialAllocationRate = 60000u;
    private const uint ActiveAllocationRate = 30000u;
#endif

    public static void VerifyNativeMethodsContract()
    {
        try
        {
            Ensure(
                !RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerSamplingInterval(0),
                "SetContinuousProfilerSamplingInterval accepted a zero interval.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerSamplingInterval(ActiveInterval),
                "SetContinuousProfilerSamplingInterval failed.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.GetContinuousProfilerSamplingInterval() == ActiveInterval,
                "GetContinuousProfilerSamplingInterval returned an unexpected value.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerEnabled(true),
                "SetContinuousProfilerEnabled failed to enable CPU sampling.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerEnabled(true),
                "SetContinuousProfilerEnabled was not idempotent.");

            Ensure(
                !RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerSnapshotSamplingInterval(0),
                "SetContinuousProfilerSnapshotSamplingInterval accepted a zero interval.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerSnapshotSamplingInterval(
                    ActiveSnapshotInterval),
                "SetContinuousProfilerSnapshotSamplingInterval failed.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerSnapshotsEnabled(true),
                "SetContinuousProfilerSnapshotsEnabled failed to enable snapshots.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerSnapshotsEnabled(true),
                "SetContinuousProfilerSnapshotsEnabled was not idempotent.");

#if NET
            Ensure(
                !RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerMaxMemorySamplesPerMinute(0),
                "SetContinuousProfilerMaxMemorySamplesPerMinute accepted a zero rate.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerMaxMemorySamplesPerMinute(
                    InitialAllocationRate),
                "SetContinuousProfilerMaxMemorySamplesPerMinute failed.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerAllocationSamplingEnabled(true),
                "SetContinuousProfilerAllocationSamplingEnabled failed to enable allocation sampling.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerAllocationSamplingEnabled(true),
                "SetContinuousProfilerAllocationSamplingEnabled was not idempotent.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerMaxMemorySamplesPerMinute(
                    ActiveAllocationRate),
                "SetContinuousProfilerMaxMemorySamplesPerMinute failed to update the active rate.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.GetContinuousProfilerAllocationSamplingRate() ==
                    ActiveAllocationRate,
                "GetContinuousProfilerAllocationSamplingRate returned an unexpected value.");
#endif

            var buffer = new byte[ThreadSamplesBufferSize];
            var read = RuntimeContinuousProfilerNativeMethods.ContinuousProfilerReadThreadSamplesV2(
                buffer.Length,
                buffer,
                out var samplingInterval);
            Ensure(read >= 0 && read <= buffer.Length, "The V2 thread sample reader returned an invalid byte count.");
            Ensure(read != 0 || samplingInterval == 0, "The V2 reader returned metadata without a sample batch.");

            var legacyRead = RuntimeContinuousProfilerNativeMethods.ContinuousProfilerReadThreadSamplesLegacy(
                buffer.Length,
                buffer);
            Ensure(
                legacyRead >= 0 && legacyRead <= buffer.Length,
                "The legacy thread sample reader returned an invalid byte count.");

            Ensure(
                RuntimeContinuousProfilerNativeMethods.ConfigureContinuousProfilerV2(
                    threadSamplingEnabled: false,
                    threadSamplingInterval: InitialInterval,
                    threadSamplingExportPipelinePrepared: true,
                    allocationSamplingEnabled: false,
#if NET
                    maxMemorySamplesPerMinute: InitialAllocationRate,
                    allocationSamplingExportPipelinePrepared: true,
#else
                    maxMemorySamplesPerMinute: 0,
                    allocationSamplingExportPipelinePrepared: false,
#endif
                    selectedThreadSamplingEnabled: false,
                    selectedThreadSamplingExportPipelinePrepared: true,
                    selectedThreadSamplingInterval: InitialSnapshotInterval,
                    out var isRepeatedConfigurationOwner),
                "A repeated ConfigureContinuousProfilerV2 call did not return the initial result.");
            Ensure(
                !isRepeatedConfigurationOwner,
                "A repeated ConfigureContinuousProfilerV2 call reported initialization ownership.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.GetContinuousProfilerSamplingInterval() == ActiveInterval,
                "A repeated ConfigureContinuousProfilerV2 call overwrote the CPU interval.");
#if NET
            Ensure(
                RuntimeContinuousProfilerNativeMethods.GetContinuousProfilerAllocationSamplingRate() ==
                    ActiveAllocationRate,
                "A repeated ConfigureContinuousProfilerV2 call overwrote allocation sampling state.");
#endif

            RuntimeContinuousProfilerNativeMethods.ConfigureContinuousProfilerLegacy(
                threadSamplingEnabled: false,
                threadSamplingInterval: InitialInterval,
                allocationSamplingEnabled: false,
                maxMemorySamplesPerMinute: 0,
                selectedThreadSamplingInterval: 0);
            Ensure(
                RuntimeContinuousProfilerNativeMethods.GetContinuousProfilerSamplingInterval() == ActiveInterval,
                "The legacy ConfigureContinuousProfiler call overwrote the active configuration.");

            Ensure(
                RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerEnabled(false),
                "SetContinuousProfilerEnabled failed to disable CPU sampling.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerEnabled(false),
                "Disabling CPU sampling was not idempotent.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerSnapshotsEnabled(false),
                "SetContinuousProfilerSnapshotsEnabled failed to disable snapshots.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerSnapshotsEnabled(false),
                "Disabling snapshots was not idempotent.");
#if NET
            Ensure(
                RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerAllocationSamplingEnabled(false),
                "SetContinuousProfilerAllocationSamplingEnabled failed to disable allocation sampling.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerAllocationSamplingEnabled(false),
                "Disabling allocation sampling was not idempotent.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.GetContinuousProfilerAllocationSamplingRate() == 0,
                "Allocation sampling remained active after it was disabled.");
#endif

            Ensure(
                RuntimeContinuousProfilerNativeMethods.ShutdownContinuousProfiler(),
                "ShutdownContinuousProfiler failed.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.ShutdownContinuousProfiler(),
                "ShutdownContinuousProfiler was not idempotent.");
            Ensure(
                !RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerEnabled(true),
                "CPU sampling was enabled after shutdown.");
            Ensure(
                !RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerSamplingInterval(ActiveInterval),
                "The CPU interval was changed after shutdown.");
            Ensure(
                !RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerSnapshotsEnabled(true),
                "Snapshots were enabled after shutdown.");
            Ensure(
                !RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerSnapshotSamplingInterval(
                    ActiveSnapshotInterval),
                "The snapshot interval was changed after shutdown.");
#if NET
            Ensure(
                !RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerAllocationSamplingEnabled(true),
                "Allocation sampling was enabled after shutdown.");
            Ensure(
                !RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerMaxMemorySamplesPerMinute(
                    ActiveAllocationRate),
                "The allocation sampling rate was changed after shutdown.");
            Ensure(
                RuntimeContinuousProfilerNativeMethods.GetContinuousProfilerAllocationSamplingRate() == 0,
                "Allocation sampling remained active after shutdown.");
#endif

            Console.WriteLine("runtime-native-methods-verified");
        }
        finally
        {
            _ = RuntimeContinuousProfilerNativeMethods.ShutdownContinuousProfiler();
        }
    }

    public static void VerifyUnpreparedPipelinesAreRejected()
    {
        try
        {
            Ensure(
                !RuntimeContinuousProfilerNativeMethods.ConfigureContinuousProfilerV2(
                    threadSamplingEnabled: false,
                    threadSamplingInterval: 0,
                    threadSamplingExportPipelinePrepared: false,
                    allocationSamplingEnabled: true,
#if NET
                    maxMemorySamplesPerMinute: InitialAllocationRate,
#else
                    maxMemorySamplesPerMinute: 1,
#endif
                    allocationSamplingExportPipelinePrepared: false,
                    selectedThreadSamplingEnabled: false,
                    selectedThreadSamplingExportPipelinePrepared: false,
                    selectedThreadSamplingInterval: 0,
                    out var isInitializationOwner),
                "ConfigureContinuousProfilerV2 accepted enabled allocation sampling without a prepared pipeline.");
            Ensure(isInitializationOwner, "The first ConfigureContinuousProfilerV2 call did not report ownership.");

            Ensure(
                !RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerSamplingInterval(UnpreparedInterval),
                "The CPU interval was configured without a prepared pipeline.");
            Ensure(
                !RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerEnabled(true),
                "CPU sampling was enabled without a prepared pipeline.");
            Ensure(
                !RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerSnapshotSamplingInterval(
                    UnpreparedInterval),
                "The snapshot interval was configured without a prepared pipeline.");
            Ensure(
                !RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerSnapshotsEnabled(true),
                "Snapshots were enabled without a prepared pipeline.");
            Ensure(
                !RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerMaxMemorySamplesPerMinute(1),
                "The allocation rate was configured without a prepared pipeline.");
            Ensure(
                !RuntimeContinuousProfilerNativeMethods.SetContinuousProfilerAllocationSamplingEnabled(true),
                "Allocation sampling was enabled without a prepared pipeline.");
#if NET
            Ensure(
                RuntimeContinuousProfilerNativeMethods.GetContinuousProfilerAllocationSamplingRate() == 0,
                "Allocation sampling started without a prepared pipeline.");
#endif

            var buffer = new byte[ThreadSamplesBufferSize];
            var read = RuntimeContinuousProfilerNativeMethods.ContinuousProfilerReadThreadSamplesV2(
                buffer.Length,
                buffer,
                out var samplingInterval);
            Ensure(read == 0, "A CPU sample was captured without a prepared pipeline.");
            Ensure(samplingInterval == 0, "A CPU sampling interval was published without a prepared pipeline.");

            Console.WriteLine("runtime-unprepared-pipelines-rejected");
        }
        finally
        {
            _ = RuntimeContinuousProfilerNativeMethods.ShutdownContinuousProfiler();
        }
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
