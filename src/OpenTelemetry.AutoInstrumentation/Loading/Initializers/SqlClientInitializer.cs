// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class SqlClientInitializer
{
    private readonly PluginManager _pluginManager;

    private int _initialized;

    public SqlClientInitializer(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager)
    {
        _pluginManager = pluginManager;
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

        var options = new OpenTelemetry.Instrumentation.SqlClient.SqlClientInstrumentationOptions();
        _pluginManager.ConfigureTracesOptions(options);

        var instrumentation = Activator.CreateInstance(instrumentationType, options)!;

        lifespanManager.Track(instrumentation);
    }
}
