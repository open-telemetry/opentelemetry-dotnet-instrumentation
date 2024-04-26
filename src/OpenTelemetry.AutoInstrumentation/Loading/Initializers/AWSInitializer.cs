// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.Instrumentation.AWS;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class AWSInitializer : InstrumentationInitializer
{
    private readonly PluginManager _pluginManager;

    public AWSInitializer(PluginManager pluginManager)
        : base("AWSSDK.Core")
    {
        _pluginManager = pluginManager;
    }

    public override void Initialize(ILifespanManager lifespanManager)
    {
        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.AWS.Implementation.AWSClientsInstrumentation, OpenTelemetry.Instrumentation.AWS")!;

        var awsClientOptions = new AWSClientInstrumentationOptions();
        _pluginManager.ConfigureTracesOptions(awsClientOptions);

        var instrumentation = Activator.CreateInstance(instrumentationType, awsClientOptions)!;
        lifespanManager.Track(instrumentation);
    }
}
