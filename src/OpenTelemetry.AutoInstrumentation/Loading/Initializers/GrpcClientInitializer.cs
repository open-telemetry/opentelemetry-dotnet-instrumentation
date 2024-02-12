// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class GrpcClientInitializer : InstrumentationInitializer
{
    private readonly PluginManager _pluginManager;

    public GrpcClientInitializer(PluginManager pluginManager)
        : base("Grpc.Net.Client")
    {
        _pluginManager = pluginManager;
    }

    public override void Initialize(ILifespanManager lifespanManager)
    {
        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.GrpcNetClient.GrpcClientInstrumentation, OpenTelemetry.Instrumentation.GrpcNetClient")!;

        var options = new OpenTelemetry.Instrumentation.GrpcNetClient.GrpcClientTraceInstrumentationOptions
        {
            SuppressDownstreamInstrumentation = !Instrumentation.TracerSettings.Value.EnabledInstrumentations.Contains(TracerInstrumentation.HttpClient)
        };

        _pluginManager.ConfigureTracesOptions(options);

        var instrumentation = Activator.CreateInstance(instrumentationType, options)!;

        lifespanManager.Track(instrumentation);
    }
}
