// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.StackExchangeRedis;

internal static class StackExchangeRedisInitializer
{
    public static void Initialize(object connection)
    {
        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.StackExchangeRedis.StackExchangeRedisConnectionInstrumentation, OpenTelemetry.Instrumentation.StackExchangeRedis")!;
        var optionsInstrumentationType = Type.GetType("OpenTelemetry.Instrumentation.StackExchangeRedis.StackExchangeRedisInstrumentationOptions, OpenTelemetry.Instrumentation.StackExchangeRedis")!;

        var options = Activator.CreateInstance(optionsInstrumentationType)!;

        Instrumentation.PluginManager?.ConfigureTracesOptions(options);

        var instrumentation = Activator.CreateInstance(instrumentationType, connection, string.Empty, options)!;

        Instrumentation.LifespanManager.Track(instrumentation);
    }
}
