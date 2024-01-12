// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class HttpClientInitializer
{
    private readonly PluginManager _pluginManager;

    private int _initialized;

    public HttpClientInitializer(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager)
    {
        _pluginManager = pluginManager;

        lazyInstrumentationLoader.Add(new GenericInitializer("System.Net.Http", InitializeOnFirstCall));

#if NETFRAMEWORK
        lazyInstrumentationLoader.Add(new GenericInitializer("System.Net", InitializeOnFirstCall));
#endif
    }

    private void InitializeOnFirstCall(ILifespanManager lifespanManager)
    {
        if (Interlocked.Exchange(ref _initialized, value: 1) != default)
        {
            // InitializeOnFirstCall() was already called before
            return;
        }

        var options = new OpenTelemetry.Instrumentation.Http.HttpClientTraceInstrumentationOptions();
        _pluginManager.ConfigureTracesOptions(options);

#if NETFRAMEWORK
        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.Http.Implementation.HttpWebRequestActivitySource, OpenTelemetry.Instrumentation.Http")!;

        instrumentationType.GetProperty("TracingOptions", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, options);
#else
        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.Http.HttpClientInstrumentation, OpenTelemetry.Instrumentation.Http")!;
        var instrumentation = Activator.CreateInstance(instrumentationType, options)!;

        lifespanManager.Track(instrumentation);
#endif
    }
}
