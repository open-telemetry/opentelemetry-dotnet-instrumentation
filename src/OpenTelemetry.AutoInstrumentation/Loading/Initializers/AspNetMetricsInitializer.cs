// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class AspNetMetricsInitializer
{
    private int _initialized;

    public AspNetMetricsInitializer(LazyInstrumentationLoader lazyInstrumentationLoader)
    {
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

        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.AspNet.AspNetMetrics, OpenTelemetry.Instrumentation.AspNet");
        var instrumentation = Activator.CreateInstance(instrumentationType);

        lifespanManager.Track(instrumentation);
    }
}

#endif
