// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal class AllocationSample
{
    public AllocationSample(long allocationSizeBytes, string allocationTypeName, ThreadSample threadSample)
    {
        AllocationSizeBytes = allocationSizeBytes;
        TypeName = allocationTypeName;
        ThreadSample = threadSample;
    }

    public long AllocationSizeBytes { get; }

    public string TypeName { get; }

    public ThreadSample ThreadSample { get; }
}
#endif
