// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class AspNetCoreInitializer : InstrumentationInitializer
{
    private readonly PluginManager _pluginManager;

    public AspNetCoreInitializer(PluginManager pluginManager)
        : base("Microsoft.AspNetCore.Http")
    {
        _pluginManager = pluginManager;
    }

    public override void Initialize(ILifespanManager lifespanManager)
    {
        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.AspNetCore.AspNetCoreInstrumentation, OpenTelemetry.Instrumentation.AspNetCore")!;
        var httpInListenerType = Type.GetType("OpenTelemetry.Instrumentation.AspNetCore.Implementation.HttpInListener, OpenTelemetry.Instrumentation.AspNetCore")!;

        var options = new OpenTelemetry.Instrumentation.AspNetCore.AspNetCoreTraceInstrumentationOptions();
        _pluginManager.ConfigureTracesOptions(options);

        var httpInListener = Activator.CreateInstance(httpInListenerType, args: options);
        var instrumentation = Activator.CreateInstance(instrumentationType, args: httpInListener)!;

        lifespanManager.Track(instrumentation);
    }
}
#endif
