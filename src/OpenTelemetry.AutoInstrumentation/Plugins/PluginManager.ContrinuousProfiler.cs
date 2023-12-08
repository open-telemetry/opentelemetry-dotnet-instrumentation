// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER

namespace OpenTelemetry.AutoInstrumentation.Plugins;

internal partial class PluginManager
{
    public Tuple<bool, uint, bool, uint>? GetFirstContinuousConfiguration()
    {
        foreach (var plugin in _plugins)
        {
            var mi = plugin.Type.GetMethod("GetContinuousProfilerConfiguration");
            if (mi is not null)
            {
                if (mi.Invoke(plugin.Instance, null) is Tuple<bool, uint, bool, uint> continuousProfilerConfiguration)
                {
                    return continuousProfilerConfiguration;
                }
            }
        }

        return null;
    }
}
#endif
