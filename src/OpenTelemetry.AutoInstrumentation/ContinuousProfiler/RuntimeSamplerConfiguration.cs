// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

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
