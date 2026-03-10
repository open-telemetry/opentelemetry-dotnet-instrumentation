// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ContinuousProfiler.ContextTracking;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
public class TestPlugin
#pragma warning restore CA1515 // Consider making public types internal Needed for AutoInstrumentation plugin loading.
{
#pragma warning disable CA1822 // Mark members as static. Needed for AutoInstrumentation plugin loading.
    public Tuple<bool, uint, bool, uint, TimeSpan, TimeSpan, object> GetContinuousProfilerConfiguration()
#pragma warning restore CA1822 // Mark members as static. Needed for AutoInstrumentation plugin loading.
    {
        var threadSamplingEnabled = true;
        var threadSamplingInterval = 1000u;
        var allocationSamplingEnabled = false;
        var maxMemorySamplesPerMinute = 200u;
        var exportInterval = TimeSpan.FromMilliseconds(500);
        var exportTimeout = TimeSpan.FromSeconds(5);
#pragma warning disable CA2000 // Dispose objects before losing scope. It should be handled by the AutoInstrumentation.
        object continuousProfilerExporter = new OtlpOverHttpExporter(TimeSpan.FromMilliseconds(threadSamplingInterval), new SampleNativeFormatParser());
#pragma warning restore CA2000 // Dispose objects before losing scope. It should be handled by the AutoInstrumentation.

        return Tuple.Create(threadSamplingEnabled, threadSamplingInterval, allocationSamplingEnabled, maxMemorySamplesPerMinute, exportInterval, exportTimeout, continuousProfilerExporter);
    }
}
