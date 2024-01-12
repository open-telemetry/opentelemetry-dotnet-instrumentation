// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ContinuousProfiler.ContextTracking;

public class TestPlugin
{
    public Tuple<bool, uint, bool, uint, TimeSpan, object> GetContinuousProfilerConfiguration()
    {
        var threadSamplingEnabled = true;
        var threadSamplingInterval = 1000u;
        var allocationSamplingEnabled = false;
        var maxMemorySamplesPerMinute = 200u;
        var exportInterval = TimeSpan.FromMilliseconds(500);
        object continuousProfilerExporter = new TestExporter();

        return Tuple.Create(threadSamplingEnabled, threadSamplingInterval, allocationSamplingEnabled, maxMemorySamplesPerMinute, exportInterval, continuousProfilerExporter);
    }
}
