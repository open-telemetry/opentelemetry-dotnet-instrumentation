// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class EntityFrameworkCoreInitializer : InstrumentationInitializer
{
    private readonly PluginManager _pluginManager;

    public EntityFrameworkCoreInitializer(PluginManager pluginManager)
        : base("Microsoft.EntityFrameworkCore")
    {
        _pluginManager = pluginManager;
    }

    public override void Initialize(ILifespanManager lifespanManager)
    {
        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.EntityFrameworkCore.EntityFrameworkInstrumentation, OpenTelemetry.Instrumentation.EntityFrameworkCore")!;

        var options = new OpenTelemetry.Instrumentation.EntityFrameworkCore.EntityFrameworkInstrumentationOptions();

        _pluginManager.ConfigureTracesOptions(options);

        var instrumentation = Activator.CreateInstance(instrumentationType, options)!;

        lifespanManager.Track(instrumentation);
    }
}
#endif
