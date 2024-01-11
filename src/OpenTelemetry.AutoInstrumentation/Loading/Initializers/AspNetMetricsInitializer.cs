// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK

using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class AspNetMetricsInitializer
{
    private readonly PluginManager _pluginManager;
    private int _initialized;

    public AspNetMetricsInitializer(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager)
    {
        _pluginManager = pluginManager;
        lazyInstrumentationLoader.Add(new AspNetMvcInitializer(InitializeOnFirstCall));
        lazyInstrumentationLoader.Add(new AspNetWebApiInitializer(InitializeOnFirstCall));
    }

    private void InitializeOnFirstCall(ILifespanManager lifespanManager)
    {
        if (Interlocked.Exchange(ref _initialized, value: 1) != default)
        {
            // InitializeOnFirstCall() was already called before
            return;
        }

        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.AspNet.AspNetMetrics, OpenTelemetry.Instrumentation.AspNet");

        var options = new OpenTelemetry.Instrumentation.AspNet.AspNetMetricsInstrumentationOptions();
        _pluginManager.ConfigureMetricsOptions(options);

        var instrumentation = Activator.CreateInstance(instrumentationType, args: options);

        lifespanManager.Track(instrumentation);
    }
}

#endif
