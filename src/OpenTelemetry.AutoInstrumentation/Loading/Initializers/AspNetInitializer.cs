// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK

using System.Diagnostics;
using System.Web;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.HeadersCapture;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class AspNetInitializer
{
    private readonly PluginManager _pluginManager;
    private readonly TracerSettings _tracerSettings;

    private int _initialized;

    public AspNetInitializer(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager, TracerSettings tracerSettings)
    {
        _pluginManager = pluginManager;
        _tracerSettings = tracerSettings;
        lazyInstrumentationLoader.Add(new AspNetMvcInitializer(InitializeOnFirstCall));
        lazyInstrumentationLoader.Add(new AspNetWebApiInitializer(InitializeOnFirstCall));
    }

    private void InitializeOnFirstCall(ILifespanManager lifespanManager)
    {
        if (Interlocked.Exchange(ref _initialized, value: 1) != default)
        {
            // InitializeOnFirstCall() was already called before
            return;
        }

        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.AspNet.AspNetInstrumentation, OpenTelemetry.Instrumentation.AspNet");

        var options = new OpenTelemetry.Instrumentation.AspNet.AspNetTraceInstrumentationOptions();

        if (_tracerSettings.InstrumentationOptions.AspNetInstrumentationCaptureRequestHeaders.Count != 0)
        {
            options.EnrichWithHttpRequest = EnrichWithHttpRequest;
        }

        if (_tracerSettings.InstrumentationOptions.AspNetInstrumentationCaptureResponseHeaders.Count != 0)
        {
            options.EnrichWithHttpResponse = EnrichWithHttpResponse;
        }

        _pluginManager.ConfigureTracesOptions(options);

        var instrumentation = Activator.CreateInstance(instrumentationType, args: options);

        lifespanManager.Track(instrumentation);
    }

    private void EnrichWithHttpRequest(Activity activity, HttpRequest httpRequest)
    {
        activity.AddHeadersAsTags(_tracerSettings.InstrumentationOptions.AspNetInstrumentationCaptureRequestHeaders, httpRequest.Headers);
    }

    private void EnrichWithHttpResponse(Activity activity, HttpResponse httpResponse)
    {
        activity.AddHeadersAsTags(_tracerSettings.InstrumentationOptions.AspNetInstrumentationCaptureResponseHeaders, httpResponse.Headers);
    }
}
#endif
