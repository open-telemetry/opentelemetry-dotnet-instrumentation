// <copyright file="PluginManager.cs" company="OpenTelemetry Authors">
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

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Plugins;

internal class PluginManager
{
    private readonly IReadOnlyList<(Type Type, object Instance)> _plugins;

    public PluginManager(GeneralSettings settings)
    {
        var plugins = new List<(Type, object)>();

        foreach (var assemblyQualifiedName in settings.Plugins)
        {
            var type = Type.GetType(assemblyQualifiedName, throwOnError: true)!;
            var instance = Activator.CreateInstance(type)!;

            plugins.Add((type, instance));
        }

        _plugins = plugins;
    }

    public void Initializing()
    {
        foreach (var plugin in _plugins)
        {
            var mi = plugin.Type.GetMethod("Initializing", Type.EmptyTypes);
            if (mi is not null)
            {
                mi.Invoke(plugin.Instance, null);
            }
        }
    }

    /// <summary>
    /// This functionality is used for testing purposes. GetAllDefinitionsPayload is not documented on the plugins.md.
    /// The contacts is based on internal classes and InternalsVisibleTo attributes. It should be refactored before supporting it.
    /// </summary>
    /// <returns>List of payloads.</returns>
    public IEnumerable<InstrumentationDefinitions.Payload> GetAllDefinitionsPayloads()
    {
        var payloads = new List<InstrumentationDefinitions.Payload>();

        foreach (var plugin in _plugins)
        {
            var mi = plugin.Type.GetMethod("GetAllDefinitionsPayload", BindingFlags.NonPublic | BindingFlags.Instance);
            if (mi is not null)
            {
                if (mi.Invoke(plugin.Instance, null) is InstrumentationDefinitions.Payload payload)
                {
                    payloads.Add(payload);
                }
            }
        }

        return payloads;
    }

    public TracerProviderBuilder BeforeConfigureTracerProviderBuilder(TracerProviderBuilder builder)
    {
        return ConfigureBuilder(builder, "BeforeConfigureTracerProvider");
    }

    public MeterProviderBuilder BeforeConfigureMeterProviderBuilder(MeterProviderBuilder builder)
    {
        return ConfigureBuilder(builder, "BeforeConfigureMeterProvider");
    }

    public TracerProviderBuilder AfterConfigureTracerProviderBuilder(TracerProviderBuilder builder)
    {
        return ConfigureBuilder(builder, "AfterConfigureTracerProvider");
    }

    public MeterProviderBuilder AfterConfigureMeterProviderBuilder(MeterProviderBuilder builder)
    {
        return ConfigureBuilder(builder, "AfterConfigureMeterProvider");
    }

    public void ConfigureMetricsOptions<T>(T options)
        where T : notnull
    {
        ConfigureOptions(options, "ConfigureMetricsOptions");
    }

    public void ConfigureTracesOptions<T>(T options)
        where T : notnull
    {
        ConfigureOptions(options, "ConfigureTracesOptions");
    }

    public void ConfigureTracesOptions(object options)
    {
        ConfigureOptions(options.GetType(), options, "ConfigureTracesOptions");
    }

    public void ConfigureLogsOptions<T>(T options)
        where T : notnull
    {
        ConfigureOptions(options, "ConfigureLogsOptions");
    }

    public ResourceBuilder ConfigureResourceBuilder(ResourceBuilder builder)
    {
        return ConfigureBuilder(builder, "ConfigureResource");
    }

    private void ConfigureOptions<T>(T options, string methodName)
        where T : notnull
    {
        ConfigureOptions(typeof(T), options, methodName);
    }

    private void ConfigureOptions(Type type, object options, string methodName)
    {
        foreach (var plugin in _plugins)
        {
            var mi = plugin.Type.GetMethod(methodName, new[] { type });
            if (mi is not null)
            {
                mi.Invoke(plugin.Instance, new[] { options });
            }
        }
    }

    private T ConfigureBuilder<T>(T builder, string methodName)
        where T : notnull
    {
        foreach (var plugin in _plugins)
        {
            var mi = plugin.Type.GetMethod(methodName, new[] { typeof(T) });
            if (mi is not null)
            {
                mi.Invoke(plugin.Instance, new object[] { builder });
            }
        }

        return builder;
    }
}
