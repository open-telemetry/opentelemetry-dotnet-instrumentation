// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi.ContinuousProfiling;
using OpenTelemetry.Resources;
using TestApplication.ContinuousProfiler.Plugins;

namespace TestApplication.ContinuousProfiler;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
public class ThreadPlugin : BasePlugin, IContinuousProfilerPlugin
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
#pragma warning disable CA1822 // Mark members as static. Needed for AutoInstrumentation plugin loading.
    public override ResourceBuilder ConfigureResource(ResourceBuilder builder)
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

    public ContinuousProfilerConfiguration GetFirstContinuousProfilerConfiguration()
    {
        var threadSamplingInterval = 1000u;

        return new ContinuousProfilerConfiguration
        {
            ThreadSamplingEnabled = true,
            ThreadSamplingInterval = threadSamplingInterval,
            AllocationSamplingEnabled = false,
            MaxMemorySamplesPerMinute = 200u,
            ExportInterval = TimeSpan.FromMilliseconds(500),
            ExportTimeout = TimeSpan.FromMilliseconds(5000),
            Exporter = new OtlpOverHttpExporter(TimeSpan.FromMilliseconds(threadSamplingInterval), new SampleNativeFormatParser())
        };
    }
}
