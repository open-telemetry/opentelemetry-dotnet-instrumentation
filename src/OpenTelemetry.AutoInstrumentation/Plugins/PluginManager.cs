// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.Telemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Plugins;

internal partial class PluginManager
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();
    private readonly List<(Type Type, IPlugin Instance)> _plugins;

    private readonly bool _hasTelemetryPlugins;

    public PluginManager(PluginsSettings settings)
    {
        var plugins = new Dictionary<Type, IPlugin>();

        foreach (var assemblyQualifiedName in settings.Plugins)
        {
            var type = Type.GetType(assemblyQualifiedName, throwOnError: true)!;

            // Ensure type implements IPlugin
            if (!typeof(IPlugin).IsAssignableFrom(type))
            {
                Logger.Warning($"Type '{type.FullName}' does not implement {nameof(IPlugin)}.");
                continue;
            }

            // Ensure type is concrete
            if (type.IsAbstract || type.IsInterface)
            {
                Logger.Warning($"Type '{type.FullName}' cannot be instantiated.");
                continue;
            }

            if (plugins.ContainsKey(type))
            {
                continue;
            }

            if (!HasDefaultConstructor(type))
            {
                Logger.Warning($"Type '{type.FullName}' cannot be instantiated. No public parameterless constructor.");
                continue;
            }

            var instance = Activator.CreateInstance(type);

            if (instance is not IPlugin plugin)
            {
                Logger.Warning($"Failed to create plugin '{type.FullName}'.");
                continue;
            }

            plugins.Add(type, plugin);
        }

        _plugins = [.. plugins.Select(it => (it.Key, it.Value))];
        _hasTelemetryPlugins = HasTelemetryPlugins(_plugins);
    }

    // Created for testing purposes.
    internal IReadOnlyList<(Type Type, IPlugin Instance)> Plugins => _plugins;

    public void Initializing()
    {
        CallPlugins<IPlugin>(plugin => plugin.Initializing());
    }

    public void Initialized()
    {
        CallPlugins<IPlugin>(plugin => plugin.Initialized());
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
        CallPlugins<ITelemetryPlugin>(plugin => plugin.TracerProviderInitialized(tracerProvider));
    }

    public void InitializedProvider(MeterProvider meterProvider)
    {
        CallPlugins<ITelemetryPlugin>(plugin => plugin.MeterProviderInitialized(meterProvider));
    }

    public TracerProviderBuilder BeforeConfigureTracerProviderBuilder(TracerProviderBuilder builder)
    {
        if (!_hasTelemetryPlugins)
        {
            return builder;
        }

        return CallPlugins<ITelemetryPlugin, TracerProviderBuilder>(plugin => plugin.BeforeConfigureTracerProvider(builder));
    }

    public MeterProviderBuilder BeforeConfigureMeterProviderBuilder(MeterProviderBuilder builder)
    {
        if (!_hasTelemetryPlugins)
        {
            return builder;
        }

        return CallPlugins<ITelemetryPlugin, MeterProviderBuilder>(plugin => plugin.BeforeConfigureMeterProvider(builder));
    }

    public TracerProviderBuilder AfterConfigureTracerProviderBuilder(TracerProviderBuilder builder)
    {
        if (!_hasTelemetryPlugins)
        {
            return builder;
        }

        return CallPlugins<ITelemetryPlugin, TracerProviderBuilder>(plugin => plugin.AfterConfigureTracerProvider(builder));
    }

    public MeterProviderBuilder AfterConfigureMeterProviderBuilder(MeterProviderBuilder builder)
    {
        if (!_hasTelemetryPlugins)
        {
            return builder;
        }

        return CallPlugins<ITelemetryPlugin, MeterProviderBuilder>(plugin => plugin.AfterConfigureMeterProvider(builder));
    }

    public void ConfigureMetricsOptions<T>(T options)
        where T : notnull
    {
        foreach (var plugin in _plugins)
        {
            if (plugin.Instance is IConfigureMetricsOptions<T> configurator)
            {
                configurator.ConfigureMetricsOptions(options);
            }
        }
    }

    public void ConfigureTracesOptions<T>(T options)
        where T : notnull
    {
        foreach (var plugin in _plugins)
        {
            if (plugin.Instance is IConfigureTracesOptions<T> configurator)
            {
                configurator.ConfigureTracesOptions(options);
            }
        }
    }

    public void ConfigureLogsOptions<T>(T options)
        where T : notnull
    {
        foreach (var plugin in _plugins)
        {
            if (plugin.Instance is IConfigureLogsOptions<T> configurator)
            {
                configurator.ConfigureLogsOptions(options);
            }
        }
    }

    public ResourceBuilder ConfigureResourceBuilder(ResourceBuilder builder)
    {
        if (!_hasTelemetryPlugins)
        {
            return builder;
        }

        return CallPlugins<ITelemetryPlugin, ResourceBuilder>(plugin => plugin.ConfigureResource(builder));
    }

    private static bool HasDefaultConstructor(Type type)
    {
        // GetConstructor returns null if a matching constructor isn't found
        return type.GetConstructor(Type.EmptyTypes) != null;
    }

    private static bool HasTelemetryPlugins(List<(Type Type, IPlugin Instance)> plugins)
    {
        foreach (var plugin in plugins)
        {
            if (plugin.Instance is ITelemetryPlugin)
            {
                return true;
            }
        }

        return false;
    }

    private void CallPlugins<TPlugin>(Action<TPlugin> action)
    {
        foreach (var plugin in _plugins)
        {
            if (plugin.Instance is TPlugin tplugin)
            {
                action(tplugin);
            }
        }
    }

    private TReturn CallPlugins<TPlugin, TReturn>(Func<TPlugin, TReturn> action)
    {
        foreach (var plugin in _plugins)
        {
            if (plugin.Instance is TPlugin tplugin)
            {
                return action(tplugin);
            }
        }

        throw new InvalidOperationException($"No plugins implementing {typeof(TPlugin).Name} registered.");
    }
}
