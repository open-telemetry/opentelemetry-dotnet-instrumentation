// <copyright file="AspNetMetricsInitializer.cs" company="OpenTelemetry Authors">
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
