// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.HeadersCapture;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class AspNetCoreInitializer : InstrumentationInitializer
{
    private readonly PluginManager _pluginManager;
    private readonly TracerSettings _tracerSettings;

    public AspNetCoreInitializer(PluginManager pluginManager, TracerSettings tracerSettings)
        : base("Microsoft.AspNetCore.Http", nameof(AspNetCoreInitializer))
    {
        _pluginManager = pluginManager;
        _tracerSettings = tracerSettings;
    }

    public override void Initialize(ILifespanManager lifespanManager)
    {
        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.AspNetCore.AspNetCoreInstrumentation, OpenTelemetry.Instrumentation.AspNetCore")!;
        var httpInListenerType = Type.GetType("OpenTelemetry.Instrumentation.AspNetCore.Implementation.HttpInListener, OpenTelemetry.Instrumentation.AspNetCore")!;

        var options = new OpenTelemetry.Instrumentation.AspNetCore.AspNetCoreTraceInstrumentationOptions();

        if (_tracerSettings.InstrumentationOptions.AspNetCoreInstrumentationCaptureRequestHeaders.Count != 0)
        {
            options.EnrichWithHttpRequest = EnrichWithHttpRequest;
        }

        if (_tracerSettings.InstrumentationOptions.AspNetCoreInstrumentationCaptureResponseHeaders.Count != 0)
        {
            options.EnrichWithHttpResponse = EnrichWithHttpResponse;
        }

        _pluginManager.ConfigureTracesOptions(options);

        var httpInListener = Activator.CreateInstance(httpInListenerType, args: options);
        var instrumentation = Activator.CreateInstance(instrumentationType, args: httpInListener)!;

        lifespanManager.Track(instrumentation);
    }

    private void EnrichWithHttpRequest(Activity activity, HttpRequest httpRequest)
    {
        activity.AddHeadersAsTags(_tracerSettings.InstrumentationOptions.AspNetCoreInstrumentationCaptureRequestHeaders, httpRequest.Headers);
    }

    private void EnrichWithHttpResponse(Activity activity, HttpResponse httpResponse)
    {
        activity.AddHeadersAsTags(_tracerSettings.InstrumentationOptions.AspNetCoreInstrumentationCaptureResponseHeaders, httpResponse.Headers);
    }
}
#endif
