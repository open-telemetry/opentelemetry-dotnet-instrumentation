// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Trace;

namespace TestApplication.SelectiveSampler.Plugins;

public class MixedModeSamplingPlugin
{
    public Tuple<bool, uint, bool, uint, TimeSpan, TimeSpan, object> GetContinuousProfilerConfiguration()
    {
        var threadSamplingEnabled = true;
        var threadSamplingInterval = 200u;
        var allocationSamplingEnabled = false;
        var maxMemorySamplesPerMinute = 200u;
        var exportInterval = TimeSpan.FromMilliseconds(50);
        var exportTimeout = TimeSpan.FromMilliseconds(5000);
        object continuousProfilerExporter = JsonConsoleExporter.Instance;

        return Tuple.Create(threadSamplingEnabled, threadSamplingInterval, allocationSamplingEnabled, maxMemorySamplesPerMinute, exportInterval, exportTimeout, continuousProfilerExporter);
    }

    public Tuple<uint, TimeSpan, TimeSpan, object> GetSelectiveSamplingConfiguration()
    {
        return SelectiveSamplerPluginHelper.GetTestConfiguration();
    }

    public TracerProviderBuilder BeforeConfigureTracerProvider(
        TracerProviderBuilder builder)
    {
        builder.AddProcessor<FrequentSamplingProcessor>();
        return builder;
    }
}
