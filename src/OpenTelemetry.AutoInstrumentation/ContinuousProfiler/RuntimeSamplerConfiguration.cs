// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal enum RuntimeSamplerAuthority : uint
{
    None = 0,
    Seed = 1,
    ControlPlane = 2
}

internal enum RuntimeSamplerApplyResult
{
    Applied = 0,
    NoChange = 1,
    IgnoredSeedAlreadyCommitted = 2,
    IgnoredLowerAuthority = 3,
    RejectedInvalidArgument = 4,
    RejectedUnsupportedLayout = 5,
    RejectedInvalidConfiguration = 6,
    RejectedUnsupportedRuntime = 7,
    ActivationFailed = 8,
    ShuttingDown = 9
}

internal enum RuntimeSamplerStateQueryResult
{
    Succeeded = 0,
    InvalidArgument = 1,
    UnsupportedLayout = 2
}

// Keep this layout in sync with continuous_profiler::RuntimeSamplerConfiguration.
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RuntimeSamplerConfiguration
{
    public const uint Size = 16;

    public uint StructureSize;
    public uint CpuSamplingIntervalMilliseconds;
    public uint SelectiveThreadSamplingIntervalMilliseconds;
    public uint MaxAllocationSamplesPerMinute;

    public RuntimeSamplerConfiguration(
        uint cpuSamplingIntervalMilliseconds,
        uint selectiveThreadSamplingIntervalMilliseconds,
        uint maxAllocationSamplesPerMinute)
    {
        StructureSize = Size;
        CpuSamplingIntervalMilliseconds = cpuSamplingIntervalMilliseconds;
        SelectiveThreadSamplingIntervalMilliseconds = selectiveThreadSamplingIntervalMilliseconds;
        MaxAllocationSamplesPerMinute = maxAllocationSamplesPerMinute;
    }

    public readonly bool AnyFeatureEnabled =>
        CpuSamplingIntervalMilliseconds != 0 ||
        SelectiveThreadSamplingIntervalMilliseconds != 0 ||
        MaxAllocationSamplesPerMinute != 0;

    public readonly bool ThreadSamplingEnabled =>
        CpuSamplingIntervalMilliseconds != 0 || SelectiveThreadSamplingIntervalMilliseconds != 0;

    public readonly bool IsValid =>
        CpuSamplingIntervalMilliseconds == 0 ||
        SelectiveThreadSamplingIntervalMilliseconds == 0 ||
        (CpuSamplingIntervalMilliseconds > SelectiveThreadSamplingIntervalMilliseconds &&
         CpuSamplingIntervalMilliseconds % SelectiveThreadSamplingIntervalMilliseconds == 0);
}

// Keep this layout in sync with continuous_profiler::RuntimeSamplerState.
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RuntimeSamplerState
{
    public const uint Size = 24;

    public uint StructureSize;
    public RuntimeSamplerAuthority Authority;
    public RuntimeSamplerConfiguration CommittedConfiguration;

    public static RuntimeSamplerState Create() => new() { StructureSize = Size };
}
