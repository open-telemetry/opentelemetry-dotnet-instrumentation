// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Plugins;

internal partial class PluginManager
{
    private readonly IReadOnlyList<(Type Type, object Instance)> _plugins;

    public PluginManager(PluginsSettings settings)
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

    // Created for testing purposes.
    internal IReadOnlyList<(Type Type, object Instance)> Plugins => _plugins;

    public void Initializing()
    {
        CallPlugins("Initializing");
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

    public void InitializedProvider(TracerProvider tracerProvider)
    {
        CallPlugins("TracerProviderInitialized", (typeof(TracerProvider), tracerProvider));
    }

    public void InitializedProvider(MeterProvider meterProvider)
    {
        CallPlugins("MeterProviderInitialized", (typeof(MeterProvider), meterProvider));
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
        CallPlugins("ConfigureMetricsOptions", (typeof(T), options));
    }

    public void ConfigureTracesOptions<T>(T options)
        where T : notnull
    {
        CallPlugins("ConfigureTracesOptions", (typeof(T), options));
    }

    public void ConfigureTracesOptions(object options)
    {
        CallPlugins("ConfigureTracesOptions", (options.GetType(), options));
    }

    public void ConfigureLogsOptions<T>(T options)
        where T : notnull
    {
        CallPlugins("ConfigureLogsOptions", (typeof(T), options));
    }

    public ResourceBuilder ConfigureResourceBuilder(ResourceBuilder builder)
    {
        return ConfigureBuilder(builder, "ConfigureResource");
    }

    private T ConfigureBuilder<T>(T builder, string methodName)
        where T : notnull
    {
        CallPlugins(methodName, (typeof(T), builder));

        return builder;
    }

    private void CallPlugins(string methodName)
    {
        foreach (var plugin in _plugins)
        {
            var mi = plugin.Type.GetMethod(methodName, Type.EmptyTypes);
            mi?.Invoke(plugin.Instance, null);
        }
    }

    private void CallPlugins(string methodName, (Type Type, object Value) arg)
    {
        foreach (var plugin in _plugins)
        {
            var mi = plugin.Type.GetMethod(methodName, [arg.Type]);
            mi?.Invoke(plugin.Instance, [arg.Value]);
        }
    }
}
