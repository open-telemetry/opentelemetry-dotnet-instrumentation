// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Resources;

namespace TestApplication.SelectiveSampler.Plugins;

public class SelectiveSamplerPlugin
{
    public Tuple<uint, TimeSpan, TimeSpan, object> GetSelectiveSamplingConfiguration()
    {
        return SelectiveSamplerPluginHelper.GetTestConfiguration();
    }

    public ResourceBuilder ConfigureResource(ResourceBuilder builder)
    {
        ResourcesProvider.Configure(builder);
        return builder;
    }
}
