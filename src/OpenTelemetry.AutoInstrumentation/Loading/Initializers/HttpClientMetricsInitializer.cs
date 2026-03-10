// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class HttpClientMetricsInitializer
{
    private int _initialized;

    public HttpClientMetricsInitializer(LazyInstrumentationLoader lazyInstrumentationLoader)
    {
        lazyInstrumentationLoader.Add(new GenericInitializer("System.Net.Http", "HttpClientMetricsInitializer", InitializeOnFirstCall));

#if NETFRAMEWORK
        lazyInstrumentationLoader.Add(new GenericInitializer("System.Net", "HttpClientMetricsInitializerForSystemNet", InitializeOnFirstCall));
#endif
    }

    private void InitializeOnFirstCall(ILifespanManager lifespanManager)
    {
        if (Interlocked.Exchange(ref _initialized, value: 1) != default)
        {
            // InitializeOnFirstCall() was already called before
            return;
        }

        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.Http.HttpClientMetrics, OpenTelemetry.Instrumentation.Http")!;
        var instrumentation = Activator.CreateInstance(instrumentationType)!;

        lifespanManager.Track(instrumentation);
    }
}
