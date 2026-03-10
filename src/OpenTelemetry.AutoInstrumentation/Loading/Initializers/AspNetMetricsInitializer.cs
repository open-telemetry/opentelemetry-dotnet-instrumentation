// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK

using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal sealed class AspNetMetricsInitializer
{
    private readonly PluginManager _pluginManager;
    private int _initialized;

    public AspNetMetricsInitializer(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager)
    {
        _pluginManager = pluginManager;
        lazyInstrumentationLoader.Add(new AspNetDirectInitializer(InitializeOnFirstCall, "AspNetDirectInitializerForMetrics"));
        lazyInstrumentationLoader.Add(new AspNetMvcInitializer(InitializeOnFirstCall, "AspNetMvcInitializerForMetrics"));
        lazyInstrumentationLoader.Add(new AspNetWebApiInitializer(InitializeOnFirstCall, "AspNetWebApiInitializerForMetrics"));
    }

    private void InitializeOnFirstCall(ILifespanManager lifespanManager)
    {
        if (Interlocked.Exchange(ref _initialized, value: 1) != 0)
        {
            // InitializeOnFirstCall() was already called before
            return;
        }

        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.AspNet.AspNetInstrumentation, OpenTelemetry.Instrumentation.AspNet");
        var instanceField = instrumentationType?.GetField("Instance");
        var instance = instanceField?.GetValue(null);
        var metricsOptionsPropertyInfo = instrumentationType?.GetProperty("MetricOptions");

        if (metricsOptionsPropertyInfo?.GetValue(instance) is OpenTelemetry.Instrumentation.AspNet.AspNetMetricsInstrumentationOptions options)
        {
            _pluginManager.ConfigureTracesOptions(options);
        }

        var handleManagerType = Type.GetType("OpenTelemetry.Instrumentation.InstrumentationHandleManager, OpenTelemetry.Instrumentation.AspNet");
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

#endif
