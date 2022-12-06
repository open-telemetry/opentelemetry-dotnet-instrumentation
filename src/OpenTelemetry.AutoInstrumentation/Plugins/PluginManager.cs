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

using System;
using System.Collections.Generic;
using OpenTelemetry.AutoInstrumentation.Configuration;
using OpenTelemetry.Metrics;
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
            var type = Type.GetType(assemblyQualifiedName, throwOnError: true);
            var instance = Activator.CreateInstance(type);

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

    public TracerProviderBuilder ConfigureTracerProviderBuilder(TracerProviderBuilder builder)
    {
        return ConfigureBuilder(builder, "ConfigureTracerProvider");
    }

    public MeterProviderBuilder ConfigureMeterProviderBuilder(MeterProviderBuilder builder)
    {
        return ConfigureBuilder(builder, "ConfigureMeterProvider");
    }

    public void ConfigureMetricsOptions<T>(T options)
    {
        ConfigureOptions(options, "ConfigureMetricsOptions");
    }

    public void ConfigureTracesOptions<T>(T options)
    {
        ConfigureOptions(options, "ConfigureTracesOptions");
    }

    public void ConfigureLogsOptions<T>(T options)
    {
        ConfigureOptions(options, "ConfigureLogsOptions");
    }

    private void ConfigureOptions<T>(T options, string methodName)
    {
        foreach (var plugin in _plugins)
        {
            var mi = plugin.Type.GetMethod(methodName, new[] { typeof(T) });
            if (mi is not null)
            {
                mi.Invoke(plugin.Instance, new object[] { options });
            }
        }
    }

    private T ConfigureBuilder<T>(T builder, string methodName)
    {
        foreach (var plugin in _plugins)
        {
            var mi = plugin.Type.GetMethod(methodName, new[] { typeof(T) });
            if (mi is not null)
            {
                builder = (T)mi.Invoke(plugin.Instance, new object[] { builder });
            }
        }

        return builder;
    }
}
