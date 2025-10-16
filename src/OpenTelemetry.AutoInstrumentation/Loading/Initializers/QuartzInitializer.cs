// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class QuartzInitializer : InstrumentationInitializer
{
    private readonly PluginManager _pluginManager;

    public QuartzInitializer(PluginManager pluginManager)
        : base("Quartz", nameof(QuartzInitializer))
    {
        _pluginManager = pluginManager;
    }

    public override void Initialize(ILifespanManager lifespanManager)
    {
        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.Quartz.QuartzJobInstrumentation, OpenTelemetry.Instrumentation.Quartz")!;

        var options = new OpenTelemetry.Instrumentation.Quartz.QuartzInstrumentationOptions();

        _pluginManager.ConfigureTracesOptions(options);

        var instrumentation = Activator.CreateInstance(instrumentationType, options)!;

        lifespanManager.Track(instrumentation);
    }
}
