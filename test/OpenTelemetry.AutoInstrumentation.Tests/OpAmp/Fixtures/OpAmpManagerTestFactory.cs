// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.OpAmp;
using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.Resources;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures;

internal static class OpAmpManagerTestFactory
{
    public static PluginManager CreatePluginManager<TPlugin>()
    {
        return CreatePluginManager(typeof(TPlugin));
    }

    public static PluginManager CreatePluginManager(params Type[] pluginTypes)
    {
        var settings = new PluginsSettings();
        foreach (var pluginType in pluginTypes)
        {
            settings.Plugins.Add(pluginType.AssemblyQualifiedName!);
        }

        return new PluginManager(settings);
    }

    public static TPlugin GetPlugin<TPlugin>(PluginManager pluginManager)
        where TPlugin : IPlugin
    {
        return Assert.IsType<TPlugin>(pluginManager.Plugins.Single(plugin => plugin.Type == typeof(TPlugin)).Instance);
    }

    public static OpAmpManager CreateManager(PluginManager pluginManager, OpAmpSettings? opAmpSettings = null)
    {
        Assert.True(OpAmpManager.TryCreate(Resource.Empty, opAmpSettings ?? new OpAmpSettings(), pluginManager, out var manager));
        return manager;
    }
}
