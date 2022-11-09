// <copyright file="AspNetInitializer.cs" company="OpenTelemetry Authors">
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

using System;
using System.Threading;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading;

internal class AspNetInitializer
{
    private readonly PluginManager _pluginManager;

    private int _firstInitialization = 1;

    public AspNetInitializer(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager)
    {
        _pluginManager = pluginManager;
        lazyInstrumentationLoader.Add(new AspNetMvcInitializer(InitializeOnFirstCall));
        lazyInstrumentationLoader.Add(new AspNetWebApiInitializer(InitializeOnFirstCall));
    }

    private void InitializeOnFirstCall(ILifespanManager lifespanManager)
    {
        if (Interlocked.Exchange(ref _firstInitialization, value: 0) != 1)
        {
            // InitializeOnFirstCall() was already called before
            return;
        }

        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.AspNet.AspNetInstrumentation, OpenTelemetry.Instrumentation.AspNet");

        var options = new OpenTelemetry.Instrumentation.AspNet.AspNetInstrumentationOptions();
        _pluginManager.ConfigureOptions(options);

        var instrumentation = Activator.CreateInstance(instrumentationType, args: options);

        lifespanManager.Track(instrumentation);
    }
}
#endif
