// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Net.Http;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.HeadersCapture;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class GrpcClientInitializer : InstrumentationInitializer
{
    private readonly PluginManager _pluginManager;
    private readonly TracerSettings _tracerSettings;

    public GrpcClientInitializer(PluginManager pluginManager, TracerSettings tracerSettings)
        : base("Grpc.Net.Client", nameof(GrpcClientInitializer))
    {
        _pluginManager = pluginManager;
        _tracerSettings = tracerSettings;
    }

    public override void Initialize(ILifespanManager lifespanManager)
    {
        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.GrpcNetClient.GrpcClientInstrumentation, OpenTelemetry.Instrumentation.GrpcNetClient")!;

        var options = new OpenTelemetry.Instrumentation.GrpcNetClient.GrpcClientTraceInstrumentationOptions
        {
            SuppressDownstreamInstrumentation = !Instrumentation.TracerSettings.Value.EnabledInstrumentations.Contains(TracerInstrumentation.HttpClient)
        };

        if (_tracerSettings.InstrumentationOptions.GrpcNetClientInstrumentationCaptureRequestMetadata.Count != 0)
        {
            options.EnrichWithHttpRequestMessage = EnrichWithHttpRequestMessage;
        }

        if (_tracerSettings.InstrumentationOptions.GrpcNetClientInstrumentationCaptureResponseMetadata.Count != 0)
        {
            options.EnrichWithHttpResponseMessage = EnrichWithHttpResponseMessage;
        }

        _pluginManager.ConfigureTracesOptions(options);

        var instrumentation = Activator.CreateInstance(instrumentationType, options)!;

        lifespanManager.Track(instrumentation);
    }

    private void EnrichWithHttpRequestMessage(Activity activity, HttpRequestMessage httpRequestMessage)
    {
        activity.AddHeadersAsTags(_tracerSettings.InstrumentationOptions.GrpcNetClientInstrumentationCaptureRequestMetadata, httpRequestMessage.Headers);
    }

    private void EnrichWithHttpResponseMessage(Activity activity, HttpResponseMessage httpResponseMessage)
    {
        activity.AddHeadersAsTags(_tracerSettings.InstrumentationOptions.GrpcNetClientInstrumentationCaptureResponseMetadata, httpResponseMessage.Headers);
    }
}
