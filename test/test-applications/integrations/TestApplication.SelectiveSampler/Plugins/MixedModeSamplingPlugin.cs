// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi.ContinuousProfiling;
using OpenTelemetry.AutoInstrumentation.PluginApi.SelectiveSampling;
using OpenTelemetry.Trace;

namespace TestApplication.SelectiveSampler.Plugins;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
public class MixedModeSamplingPlugin : BasePlugin, ISelectiveSamplerPlugin, IContinuousProfilerPlugin
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
    public ContinuousProfilerConfiguration GetFirstContinuousProfilerConfiguration()
    {
        return new ContinuousProfilerConfiguration()
        {
            ThreadSamplingEnabled = true,
            ThreadSamplingInterval = 200u,
            AllocationSamplingEnabled = false,
            MaxMemorySamplesPerMinute = 200u,
            ExportInterval = TimeSpan.FromMilliseconds(50),
            ExportTimeout = TimeSpan.FromMilliseconds(5000),
            Exporter = JsonConsoleExporter.Instance
        };
    }

    public SelectiveSamplerConfiguration? GetFirstSelectiveSamplingConfiguration()
    {
        return SelectiveSamplerPluginHelper.GetTestConfiguration();
    }

    public override TracerProviderBuilder BeforeConfigureTracerProvider(TracerProviderBuilder builder)
    {
        builder.AddProcessor<FrequentSamplingProcessor>();
        return builder;
    }
}
