// <copyright file="HttpClientMetricsInitializer.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class HttpClientMetricsInitializer
{
    private int _initialized;

    public HttpClientMetricsInitializer(LazyInstrumentationLoader lazyInstrumentationLoader)
    {
        lazyInstrumentationLoader.Add(new GenericInitializer("System.Net.Http", InitializeOnFirstCall));

#if NETFRAMEWORK
        lazyInstrumentationLoader.Add(new GenericInitializer("System.Net", InitializeOnFirstCall));
#endif
    }

    private void InitializeOnFirstCall(ILifespanManager lifespanManager)
    {
        if (Interlocked.Exchange(ref _initialized, value: 1) != default)
        {
            // InitializeOnFirstCall() was already called before
            return;
        }

        var optionsType = Type.GetType("OpenTelemetry.Instrumentation.Http.HttpClientMetricInstrumentationOptions, OpenTelemetry.Instrumentation.Http")!;
        var options = Activator.CreateInstance(optionsType)!;

        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.Http.HttpClientMetrics, OpenTelemetry.Instrumentation.Http")!;
        var instrumentation = Activator.CreateInstance(instrumentationType, options)!;

        lifespanManager.Track(instrumentation);
    }
}
