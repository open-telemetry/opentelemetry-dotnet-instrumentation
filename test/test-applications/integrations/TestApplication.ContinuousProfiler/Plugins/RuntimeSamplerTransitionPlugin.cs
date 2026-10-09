// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi.ContinuousProfiling;
using OpenTelemetry.Resources;
using TestApplication.ContinuousProfiler.Plugins;

namespace TestApplication.ContinuousProfiler;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
public class RuntimeSamplerTransitionPlugin : BasePlugin, IContinuousProfilerPlugin
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

        ResourcesProvider.Configure(builder);
        return builder;
    }

    public ContinuousProfilerConfiguration GetFirstContinuousProfilerConfiguration()
    {
        var threadSamplingInterval = 500u;
        const bool threadSamplingEnabled = false;
#if NET
        const uint maxMemorySamplesPerMinute = 6000;
#else
        const uint maxMemorySamplesPerMinute = 0;
#endif

        return new ContinuousProfilerConfiguration
        {
            ThreadSamplingEnabled = threadSamplingEnabled,
            ThreadSamplingInterval = threadSamplingInterval,
            // Non-zero disabled settings prepare both managed pipelines before the ControlPlane snapshot enables
            // their native producers.
            AllocationSamplingEnabled = false,
            MaxMemorySamplesPerMinute = maxMemorySamplesPerMinute,
            ExportInterval = TimeSpan.FromMilliseconds(250),
            ExportTimeout = TimeSpan.FromMilliseconds(5000),
            Exporter = new OtlpOverHttpExporter(new SampleNativeFormatParser())
        };
    }
}
