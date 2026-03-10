// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Reflection;
#endif
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.HeadersCapture;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class HttpClientInitializer
{
    private readonly PluginManager _pluginManager;
    private readonly TracerSettings _tracerSettings;

    private int _initialized;

    public HttpClientInitializer(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager, TracerSettings tracerSettings)
    {
        _pluginManager = pluginManager;
        _tracerSettings = tracerSettings;

        lazyInstrumentationLoader.Add(new GenericInitializer("System.Net.Http", "HttpClientInitializerForSystemNetHttp", InitializeOnFirstCall));

#if NETFRAMEWORK
        lazyInstrumentationLoader.Add(new GenericInitializer("System.Net", "HttpClientInitializerForSystemNet", InitializeOnFirstCall));
#endif
    }

    private void InitializeOnFirstCall(ILifespanManager lifespanManager)
    {
        if (Interlocked.Exchange(ref _initialized, value: 1) != default)
        {
            // InitializeOnFirstCall() was already called before
            return;
        }

        var options = new OpenTelemetry.Instrumentation.Http.HttpClientTraceInstrumentationOptions();

        if (_tracerSettings.InstrumentationOptions.HttpInstrumentationCaptureRequestHeaders.Count != 0)
        {
            options.EnrichWithHttpRequestMessage = EnrichWithHttpRequestMessage;
            options.EnrichWithHttpWebRequest = EnrichWithHttpWebRequest;
        }

        if (_tracerSettings.InstrumentationOptions.HttpInstrumentationCaptureResponseHeaders.Count != 0)
        {
            options.EnrichWithHttpResponseMessage = EnrichWithHttpResponseMessage;
            options.EnrichWithHttpWebResponse = EnrichWithHttpWebResponse;
        }

        _pluginManager.ConfigureTracesOptions(options);

#if NETFRAMEWORK
        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.Http.Implementation.HttpWebRequestActivitySource, OpenTelemetry.Instrumentation.Http")!;

        instrumentationType.GetProperty("TracingOptions", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, options);
#else
        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.Http.HttpClientInstrumentation, OpenTelemetry.Instrumentation.Http")!;
        var instrumentation = Activator.CreateInstance(instrumentationType, options)!;

        lifespanManager.Track(instrumentation);
#endif
    }

    private void EnrichWithHttpRequestMessage(Activity activity, HttpRequestMessage httpRequestMessage)
    {
        activity.AddHeadersAsTags(_tracerSettings.InstrumentationOptions.HttpInstrumentationCaptureRequestHeaders, httpRequestMessage.Headers);
    }

    private void EnrichWithHttpWebRequest(Activity activity, HttpWebRequest httpWebRequest)
    {
        activity.AddHeadersAsTags(_tracerSettings.InstrumentationOptions.HttpInstrumentationCaptureRequestHeaders, httpWebRequest.Headers);
    }

    private void EnrichWithHttpResponseMessage(Activity activity, HttpResponseMessage httpResponseMessage)
    {
        activity.AddHeadersAsTags(_tracerSettings.InstrumentationOptions.HttpInstrumentationCaptureResponseHeaders, httpResponseMessage.Headers);
    }

    private void EnrichWithHttpWebResponse(Activity activity, HttpWebResponse httpWebResponse)
    {
        activity.AddHeadersAsTags(_tracerSettings.InstrumentationOptions.HttpInstrumentationCaptureResponseHeaders, httpWebResponse.Headers);
    }
}
