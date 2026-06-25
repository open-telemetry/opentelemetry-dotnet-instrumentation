// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal sealed class SqlClientTracerInitializer : SqlClientInitializer
{
    private readonly PluginManager _pluginManager;

    private int _initialized;

    public SqlClientTracerInitializer(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager)
        : base(lazyInstrumentationLoader, nameof(SqlClientTracerInitializer))
    {
        _pluginManager = pluginManager;
    }

    protected override void InitializeOnFirstCall(ILifespanManager lifespanManager)
    {
        if (Interlocked.Exchange(ref _initialized, value: 1) != 0)
        {
            // InitializeOnFirstCall() was already called before
            return;
        }

        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.SqlClient.SqlClientInstrumentation, OpenTelemetry.Instrumentation.SqlClient")!;
        var instanceField = instrumentationType?.GetField("Instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var instance = instanceField?.GetValue(null);

        var options = new OpenTelemetry.Instrumentation.SqlClient.SqlClientTraceInstrumentationOptions();
        _pluginManager.ConfigureTracesOptions(options);

        var addTracingHandleMethod = instrumentationType?.GetMethod("AddTracingHandle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var tracingHandle = addTracingHandleMethod?.Invoke(instance, [options]);

        if (tracingHandle != null)
        {
            lifespanManager.Track(tracingHandle);
        }
    }
}
