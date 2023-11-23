// <copyright file="HttpClientInitializer.cs" company="OpenTelemetry Authors">
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

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class HttpClientInitializer
{
    private readonly PluginManager _pluginManager;

    private int _initialized;

    public HttpClientInitializer(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager)
    {
        _pluginManager = pluginManager;

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

        var options = new OpenTelemetry.Instrumentation.Http.HttpClientInstrumentationOptions();
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
}
