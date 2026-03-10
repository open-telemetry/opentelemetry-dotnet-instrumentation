// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK

using System.Diagnostics;
using System.Web;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.HeadersCapture;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal sealed class AspNetInitializer
{
    private readonly PluginManager _pluginManager;
    private readonly TracerSettings _tracerSettings;

    private int _initialized;

    public AspNetInitializer(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager, TracerSettings tracerSettings)
    {
        _pluginManager = pluginManager;
        _tracerSettings = tracerSettings;
        lazyInstrumentationLoader.Add(new AspNetDirectInitializer(InitializeOnFirstCall, "AspNetDirectInitializerForTraces"));
        lazyInstrumentationLoader.Add(new AspNetMvcInitializer(InitializeOnFirstCall, "AspNetMvcInitializerForTraces"));
        lazyInstrumentationLoader.Add(new AspNetWebApiInitializer(InitializeOnFirstCall, "AspNetWebApiInitializerForTraces"));
    }

    private void InitializeOnFirstCall(ILifespanManager lifespanManager)
    {
        if (Interlocked.Exchange(ref _initialized, value: 1) != 0)
        {
            // InitializeOnFirstCall() was already called before
            return;
        }

        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.AspNet.AspNetInstrumentation, OpenTelemetry.Instrumentation.AspNet");
        var instanceField = instrumentationType?.GetField("Instance");
        var instance = instanceField?.GetValue(null);
        var traceOptionsProperty = instrumentationType?.GetProperty("TraceOptions");

        if (traceOptionsProperty?.GetValue(instance) is OpenTelemetry.Instrumentation.AspNet.AspNetTraceInstrumentationOptions options)
        {
            if (_tracerSettings.InstrumentationOptions.AspNetInstrumentationCaptureRequestHeaders.Count != 0)
            {
                options.EnrichWithHttpRequest = EnrichWithHttpRequest;
            }

            if (_tracerSettings.InstrumentationOptions.AspNetInstrumentationCaptureResponseHeaders.Count != 0)
            {
                options.EnrichWithHttpResponse = EnrichWithHttpResponse;
            }

            _pluginManager.ConfigureTracesOptions(options);
        }

        var handleManagerType = Type.GetType("OpenTelemetry.Instrumentation.InstrumentationHandleManager, OpenTelemetry.Instrumentation.AspNet");
        var handleManagerField = instrumentationType?.GetField("HandleManager");
        var handleManager = handleManagerField?.GetValue(instance);
        var addTracingHandleMethod = handleManagerType?.GetMethod("AddTracingHandle");
        var tracingHandle = addTracingHandleMethod?.Invoke(handleManager, []);

        if (tracingHandle != null)
        {
           lifespanManager.Track(tracingHandle);
        }
    }

    private void EnrichWithHttpRequest(Activity activity, HttpRequestBase httpRequest)
    {
        activity.AddHeadersAsTags(_tracerSettings.InstrumentationOptions.AspNetInstrumentationCaptureRequestHeaders, httpRequest.Headers);
    }

    private void EnrichWithHttpResponse(Activity activity, HttpResponseBase httpResponse)
    {
        activity.AddHeadersAsTags(_tracerSettings.InstrumentationOptions.AspNetInstrumentationCaptureResponseHeaders, httpResponse.Headers);
    }
}
#endif
