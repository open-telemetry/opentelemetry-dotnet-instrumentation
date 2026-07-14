// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi.SelectiveSampling;
using OpenTelemetry.Trace;

namespace TestApplication.SelectiveSampler.Plugins;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
public class SelectiveSamplerPlugin : BasePlugin, ISelectiveSamplerPlugin
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
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
