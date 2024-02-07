// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class EntityFrameworkCoreInitializer : InstrumentationInitializer
{
    private readonly PluginManager _pluginManager;
    private readonly TracerSettings _tracerSettings;

    public EntityFrameworkCoreInitializer(PluginManager pluginManager, TracerSettings tracerSettings)
        : base("Microsoft.EntityFrameworkCore")
    {
        _pluginManager = pluginManager;
        _tracerSettings = tracerSettings;
    }

    public override void Initialize(ILifespanManager lifespanManager)
    {
        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.EntityFrameworkCore.EntityFrameworkInstrumentation, OpenTelemetry.Instrumentation.EntityFrameworkCore")!;

        var options = new OpenTelemetry.Instrumentation.EntityFrameworkCore.EntityFrameworkInstrumentationOptions
        {
            SetDbStatementForText = _tracerSettings.InstrumentationOptions.EntityFrameworkCoreSetDbStatementForText
        };

        _pluginManager.ConfigureTracesOptions(options);

        var instrumentation = Activator.CreateInstance(instrumentationType, options)!;

        lifespanManager.Track(instrumentation);
    }
}
#endif
