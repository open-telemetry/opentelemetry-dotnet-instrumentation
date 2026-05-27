// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi.ContinuousProfiling;

namespace OpenTelemetry.AutoInstrumentation.Plugins;

internal partial class PluginManager
{
    public ContinuousProfilerConfiguration GetFirstContinousProfilerConfiguration()
    {
        foreach (var plugin in _plugins)
        {
            if (plugin.Instance is IContinuousProfilerPlugin profilerPlugin)
            {
                return profilerPlugin.GetFirstContinuousProfilerConfiguration();
            }
        }

        return ContinuousProfilerConfiguration.Default;
    }
}
