// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Resources;
using TestApplication.ContinuousProfiler;

namespace TestApplication.ContinuousProfiler.Contention;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
public class ContentionPlugin
#pragma warning restore CA1515
{
#pragma warning disable CA1822 // Mark members as static. Needed for AutoInstrumentation plugin loading.
    public Tuple<bool, uint, bool, uint, TimeSpan, TimeSpan, object> GetContinuousProfilerConfiguration()
    {
        var threadSamplingEnabled = true;
        // 1 second sampling interval - fast enough to catch the deadlock within the
        // persistence gate window (2 consecutive ticks with the cycle present).
        var threadSamplingInterval = 1000u;
        var allocationSamplingEnabled = false;
        var maxMemorySamplesPerMinute = 200u;
        var exportInterval = TimeSpan.FromMilliseconds(500);
        var exportTimeout = TimeSpan.FromMilliseconds(5000);
#pragma warning disable CA2000 // Dispose objects before losing scope.
        object continuousProfilerExporter = new OtlpOverHttpExporter(
            TimeSpan.FromMilliseconds(threadSamplingInterval),
            new SampleNativeFormatParser());
#pragma warning restore CA2000

        return Tuple.Create(
            threadSamplingEnabled,
            threadSamplingInterval,
            allocationSamplingEnabled,
            maxMemorySamplesPerMinute,
            exportInterval,
            exportTimeout,
            continuousProfilerExporter);
    }

    public ResourceBuilder ConfigureResource(ResourceBuilder builder)
#pragma warning restore CA1822
    {
        // ArgumentNullException.ThrowIfNull(builder);

        ResourcesProvider.Configure(builder);
        return builder;
    }
}
