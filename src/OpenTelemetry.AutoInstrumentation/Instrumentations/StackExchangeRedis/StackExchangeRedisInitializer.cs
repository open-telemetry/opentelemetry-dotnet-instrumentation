// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.StackExchangeRedis;

internal static class StackExchangeRedisInitializer
{
    public static void Initialize(object connection)
    {
        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.StackExchangeRedis.StackExchangeRedisConnectionInstrumentation, OpenTelemetry.Instrumentation.StackExchangeRedis")!;
        var optionsInstrumentationType = Type.GetType("OpenTelemetry.Instrumentation.StackExchangeRedis.StackExchangeRedisInstrumentationOptions, OpenTelemetry.Instrumentation.StackExchangeRedis")!;

        var options = Activator.CreateInstance(optionsInstrumentationType)!;
        var pluginManager = Instrumentation.PluginManager;

        if (pluginManager != null)
        {
            InvokeConfigureOptions(pluginManager, optionsInstrumentationType, options);
        }

        var instrumentation = Activator.CreateInstance(instrumentationType, connection, string.Empty, options)!;

        Instrumentation.LifespanManager.Track(instrumentation);
    }

    private static void InvokeConfigureOptions(PluginManager pluginManager, Type optionsType, object options)
    {
        var method = typeof(PluginManager)
            .GetMethod(nameof(PluginManager.ConfigureTracesOptions))!
            .MakeGenericMethod(optionsType);

        method.Invoke(
            pluginManager,
            [options]);
    }
}
