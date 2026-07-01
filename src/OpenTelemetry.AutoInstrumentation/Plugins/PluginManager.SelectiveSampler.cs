// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
using OpenTelemetry.AutoInstrumentation.PluginApi.SelectiveSampling;

namespace OpenTelemetry.AutoInstrumentation.Plugins;

internal partial class PluginManager
{
    public SelectiveSamplerConfiguration? GetFirstSelectiveSamplingConfiguration()
    {
        foreach (var plugin in _plugins)
        {
            if (plugin.Instance is ISelectiveSamplerPlugin samplerPlugin)
            {
                return samplerPlugin.GetFirstSelectiveSamplingConfiguration();
            }
        }

        return null;
    }
}
