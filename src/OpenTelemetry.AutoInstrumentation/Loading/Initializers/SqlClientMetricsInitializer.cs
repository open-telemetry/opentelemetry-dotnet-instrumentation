// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal sealed class SqlClientMetricsInitializer : SqlClientInitializer
{
    private readonly PluginManager _pluginManager;

    private int _initialized;

    public SqlClientMetricsInitializer(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager)
        : base(lazyInstrumentationLoader, nameof(SqlClientMetricsInitializer))
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
        var instanceField = instrumentationType?.GetField("Instance");
        var instance = instanceField?.GetValue(null);
        var metricsOptionsPropertyInfo = instrumentationType?.GetProperty("MetricOptions");

        if (metricsOptionsPropertyInfo?.GetValue(instance) is OpenTelemetry.Instrumentation.SqlClient.SqlClientTraceInstrumentationOptions options)
        {
            _pluginManager.ConfigureTracesOptions(options);
        }

        var handleManagerType = Type.GetType("OpenTelemetry.Instrumentation.InstrumentationHandleManager, OpenTelemetry.Instrumentation.SqlClient");
        var handleManagerField = instrumentationType?.GetField("HandleManager");
        var handleManager = handleManagerField?.GetValue(instance);
        var addMetricHandleMethod = handleManagerType?.GetMethod("AddMetricHandle");
        var metricHandle = addMetricHandleMethod?.Invoke(handleManager, []);

        if (metricHandle != null)
        {
            lifespanManager.Track(metricHandle);
        }
    }
}
