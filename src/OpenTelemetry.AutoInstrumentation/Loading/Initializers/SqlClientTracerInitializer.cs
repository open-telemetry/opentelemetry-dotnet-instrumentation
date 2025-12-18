// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal sealed class SqlClientTracerInitializer : SqlClientInitializer
{
    private readonly PluginManager _pluginManager;
#if NETFRAMEWORK
    private readonly TracerSettings _tracerSettings;
#endif
    private int _initialized;

    public SqlClientTracerInitializer(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager, TracerSettings tracerSettings)
        : base(lazyInstrumentationLoader, nameof(SqlClientTracerInitializer))
    {
        _pluginManager = pluginManager;
#if NETFRAMEWORK
        _tracerSettings = tracerSettings;
#endif
    }

    protected override void InitializeOnFirstCall(ILifespanManager lifespanManager)
    {
        if (Interlocked.Exchange(ref _initialized, value: 1) != 0)
        {
            // InitializeOnFirstCall() was already called before
            return;
        }

#if NETFRAMEWORK
        NativeMethods.SetSqlClientNetFxILRewriteEnabled(_tracerSettings.InstrumentationOptions.SqlClientNetFxIlRewriteEnabled);
#endif

        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.SqlClient.SqlClientInstrumentation, OpenTelemetry.Instrumentation.SqlClient")!;
        var instanceField = instrumentationType?.GetField("Instance");
        var instance = instanceField?.GetValue(null);
        var traceOptionsProperty = instrumentationType?.GetProperty("TraceOptions");

        if (traceOptionsProperty?.GetValue(instance) is OpenTelemetry.Instrumentation.SqlClient.SqlClientTraceInstrumentationOptions options)
        {
            _pluginManager.ConfigureTracesOptions(options);
        }

        var handleManagerType = Type.GetType("OpenTelemetry.Instrumentation.InstrumentationHandleManager, OpenTelemetry.Instrumentation.SqlClient");
        var handleManagerField = instrumentationType?.GetField("HandleManager");
        var handleManager = handleManagerField?.GetValue(instance);
        var addTracingHandleMethod = handleManagerType?.GetMethod("AddTracingHandle");
        var tracingHandle = addTracingHandleMethod?.Invoke(handleManager, []);

        if (tracingHandle != null)
        {
            lifespanManager.Track(tracingHandle);
        }
    }
}
