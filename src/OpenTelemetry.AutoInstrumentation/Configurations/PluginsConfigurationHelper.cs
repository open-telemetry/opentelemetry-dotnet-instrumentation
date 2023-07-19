// <copyright file="PluginsConfigurationHelper.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

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
