// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;

namespace OpenTelemetry.AutoInstrumentation.Plugins;

internal partial class PluginManager
{
    public IReadOnlyList<(Type Type, IOpAmpPlugin Instance)> GetOpAmpPlugins()
    {
        var plugins = new List<(Type Type, IOpAmpPlugin Instance)>();

        foreach (var plugin in _plugins)
        {
            if (plugin.Instance is IOpAmpPlugin opAmpPlugin)
            {
                plugins.Add((plugin.Type, opAmpPlugin));
            }
        }

        return plugins;
    }
}
