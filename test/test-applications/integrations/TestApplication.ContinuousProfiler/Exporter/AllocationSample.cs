// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ContinuousProfiler;

internal sealed class AllocationSample
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
