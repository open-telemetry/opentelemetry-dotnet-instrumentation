// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal static class PluginsConfigurationHelper
{
    public static TracerProviderBuilder InvokePluginsBefore(this TracerProviderBuilder builder, PluginManager pluginManager)
    {
        return pluginManager.BeforeConfigureTracerProviderBuilder(builder);
    }

    public static MeterProviderBuilder InvokePluginsBefore(this MeterProviderBuilder builder, PluginManager pluginManager)
    {
        return pluginManager.BeforeConfigureMeterProviderBuilder(builder);
    }

    public static TracerProviderBuilder InvokePluginsAfter(this TracerProviderBuilder builder, PluginManager pluginManager)
    {
        return pluginManager.AfterConfigureTracerProviderBuilder(builder);
    }

    public static MeterProviderBuilder InvokePluginsAfter(this MeterProviderBuilder builder, PluginManager pluginManager)
    {
        return pluginManager.AfterConfigureMeterProviderBuilder(builder);
    }

    public static ResourceBuilder InvokePlugins(this ResourceBuilder builder, PluginManager pluginManager)
    {
        return pluginManager.ConfigureResourceBuilder(builder);
    }

    public static void TryCallInitialized(this TracerProvider? provider, PluginManager pluginManager)
    {
        if (provider is not null)
        {
            pluginManager.InitializedProvider(provider);
        }
    }

    public static void TryCallInitialized(this MeterProvider? provider, PluginManager pluginManager)
    {
        if (provider is not null)
        {
            pluginManager.InitializedProvider(provider);
        }
    }
}
