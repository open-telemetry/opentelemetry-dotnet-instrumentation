// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ContinuousProfiler;

public class Plugin
{
    public Tuple<bool, uint, bool, uint, TimeSpan, object> GetContinuousProfilerConfiguration()
    {
        var threadSamplingEnabled = true;
        var threadSamplingInterval = 1000u;
        var allocationSamplingEnabled = true;
        var maxMemorySamplesPerMinute = 200u;
        var exportInterval = TimeSpan.FromMilliseconds(threadSamplingInterval);
        object continuousProfilerExporter = new ConsoleExporter();

        return Tuple.Create(threadSamplingEnabled, threadSamplingInterval, allocationSamplingEnabled, maxMemorySamplesPerMinute, exportInterval, continuousProfilerExporter);
    }
}
