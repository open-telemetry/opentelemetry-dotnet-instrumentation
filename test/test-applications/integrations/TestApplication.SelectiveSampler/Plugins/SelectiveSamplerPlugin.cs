// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Trace;

namespace TestApplication.SelectiveSampler.Plugins;

public class SelectiveSamplerPlugin
{
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
