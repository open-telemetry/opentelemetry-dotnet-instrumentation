// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Plugins;

internal partial class PluginManager
{
    public (bool ThreadSamplingEnabled, uint ThreadSamplingInterval, bool AllocationSamplingEnabled, uint
        MaxMemorySamplesPerMinute, TimeSpan ExportInterval, TimeSpan ExportTimeout, object ContinuousProfilerExporter)
        GetFirstContinuousConfiguration()
    {
        foreach (var plugin in _plugins)
        {
            var mi = plugin.Type.GetMethod("GetContinuousProfilerConfiguration");
            if (mi?.Invoke(plugin.Instance, null) is Tuple<bool, uint, bool, uint, TimeSpan, TimeSpan, object>
                continuousProfilerConfiguration)
            {
                return (continuousProfilerConfiguration.Item1, continuousProfilerConfiguration.Item2,
                    continuousProfilerConfiguration.Item3, continuousProfilerConfiguration.Item4,
                    continuousProfilerConfiguration.Item5, continuousProfilerConfiguration.Item6,
                    continuousProfilerConfiguration.Item7);
            }
        }

        return (false, 0u, false, 0u, TimeSpan.Zero, TimeSpan.Zero, new object());
    }
}
