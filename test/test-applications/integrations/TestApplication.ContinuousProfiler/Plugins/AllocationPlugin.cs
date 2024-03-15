// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ContinuousProfiler;

public class AllocationPlugin
{
    public Tuple<bool, uint, bool, uint, TimeSpan, object> GetContinuousProfilerConfiguration()
    {
        var threadSamplingEnabled = false;
        var threadSamplingInterval = 1000u;
        var allocationSamplingEnabled = true;
        var maxMemorySamplesPerMinute = 200u;
        var exportInterval = TimeSpan.FromMilliseconds(500);
        object continuousProfilerExporter = new OtlpOverHttpExporter();

        return Tuple.Create(threadSamplingEnabled, threadSamplingInterval, allocationSamplingEnabled, maxMemorySamplesPerMinute, exportInterval, continuousProfilerExporter);
    }
}
