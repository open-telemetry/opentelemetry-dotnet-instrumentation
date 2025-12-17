// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
namespace OpenTelemetry.AutoInstrumentation.Plugins;

internal partial class PluginManager
{
    public (uint SelectiveSamplingInterval, TimeSpan ExportInterval, TimeSpan ExportTimeou, object? SelectiveSamplingExporter)? GetFirstSelectiveSamplingConfiguration()
    {
        foreach (var plugin in _plugins)
        {
            var mi = plugin.Type.GetMethod("GetSelectiveSamplingConfiguration");
            if (mi?.Invoke(plugin.Instance, null) is Tuple<uint, TimeSpan, TimeSpan, object?> selectiveSamplingConfiguration)
            {
                return (selectiveSamplingConfiguration.Item1, selectiveSamplingConfiguration.Item2, selectiveSamplingConfiguration.Item3, selectiveSamplingConfiguration.Item4);
            }
        }

        return null;
    }
}
