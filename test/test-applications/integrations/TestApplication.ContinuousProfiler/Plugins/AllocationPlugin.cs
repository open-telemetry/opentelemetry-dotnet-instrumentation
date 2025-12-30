// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Resources;

namespace TestApplication.ContinuousProfiler;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
public class AllocationPlugin
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
#pragma warning disable CA1822 // Mark members as static. Needed for AutoInstrumentation plugin loading.
    public Tuple<bool, uint, bool, uint, TimeSpan, TimeSpan, object> GetContinuousProfilerConfiguration()
    {
        var threadSamplingEnabled = false;
        var threadSamplingInterval = 1000u;
        var allocationSamplingEnabled = true;
        var maxMemorySamplesPerMinute = 200u;
        var exportInterval = TimeSpan.FromMilliseconds(500);
        var exportTimeout = TimeSpan.FromMilliseconds(5000);
#pragma warning disable CA2000 // Dispose objects before losing scope. It should be handled by the AutoInstrumentation.
        object continuousProfilerExporter = new OtlpOverHttpExporter(TimeSpan.FromMilliseconds(threadSamplingInterval), new SampleNativeFormatParser());
#pragma warning restore CA2000 // Dispose objects before losing scope. It should be handled by the AutoInstrumentation.

        return Tuple.Create(threadSamplingEnabled, threadSamplingInterval, allocationSamplingEnabled, maxMemorySamplesPerMinute, exportInterval, exportTimeout, continuousProfilerExporter);
    }

    public ResourceBuilder ConfigureResource(ResourceBuilder builder)
#pragma warning restore CA1822 // Mark members as static. Needed for AutoInstrumentation plugin loading.
    {
#if NET
        ArgumentNullException.ThrowIfNull(builder);
#else
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }
#endif

        // TODO hack to fetch resources from the SDK and store them to be able to sent to OTLP Profiling
        ResourcesProvider.Configure(builder);
        return builder;
    }
}
