// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class SqlClientInitializer
{
    private readonly PluginManager _pluginManager;
    private readonly TracerSettings _tracerSettings;

    private int _initialized;

    public SqlClientInitializer(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager, TracerSettings tracerSettings)
    {
        _pluginManager = pluginManager;
        _tracerSettings = tracerSettings;
        lazyInstrumentationLoader.Add(new GenericInitializer("System.Data.SqlClient", InitializeOnFirstCall));
        lazyInstrumentationLoader.Add(new GenericInitializer("Microsoft.Data.SqlClient", InitializeOnFirstCall));

#if NETFRAMEWORK
        lazyInstrumentationLoader.Add(new GenericInitializer("System.Data", InitializeOnFirstCall));
#endif
    }

    private void InitializeOnFirstCall(ILifespanManager lifespanManager)
    {
        if (Interlocked.Exchange(ref _initialized, value: 1) != default)
        {
            // InitializeOnFirstCall() was already called before
            return;
        }

        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.SqlClient.SqlClientInstrumentation, OpenTelemetry.Instrumentation.SqlClient")!;

        var options = new OpenTelemetry.Instrumentation.SqlClient.SqlClientTraceInstrumentationOptions
        {
            SetDbStatementForText = _tracerSettings.InstrumentationOptions.SqlClientSetDbStatementForText
        };
        _pluginManager.ConfigureTracesOptions(options);

        var propertyInfo = instrumentationType.GetProperty("TracingOptions", BindingFlags.Static | BindingFlags.Public);
        propertyInfo?.SetValue(null, options);

        var instrumentation = instrumentationType.InvokeMember("AddTracingHandle", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, Type.DefaultBinder, null, []);

        if (instrumentation != null)
        {
            lifespanManager.Track(instrumentation);
        }
    }
}
