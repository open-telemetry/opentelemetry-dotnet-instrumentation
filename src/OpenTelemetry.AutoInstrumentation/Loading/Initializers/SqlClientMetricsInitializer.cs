// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal sealed class SqlClientMetricsInitializer : SqlClientInitializer
{
    private readonly PluginManager _pluginManager;

    private int _initialized;

    public SqlClientMetricsInitializer(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager)
        : base(lazyInstrumentationLoader)
    {
        _pluginManager = pluginManager;
    }

    protected override void InitializeOnFirstCall(ILifespanManager lifespanManager)
    {
        if (Interlocked.Exchange(ref _initialized, value: 1) != default)
        {
            // InitializeOnFirstCall() was already called before
            return;
        }

        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.SqlClient.SqlClientInstrumentation, OpenTelemetry.Instrumentation.SqlClient")!;
        var instrumentation = instrumentationType.InvokeMember("AddMetricHandle", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, Type.DefaultBinder, null, []);

        if (instrumentation != null)
        {
            lifespanManager.Track(instrumentation);
        }
    }
}
