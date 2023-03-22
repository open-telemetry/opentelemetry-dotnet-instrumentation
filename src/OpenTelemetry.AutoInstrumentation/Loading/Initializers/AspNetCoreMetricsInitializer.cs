// <copyright file="AspNetCoreMetricsInitializer.cs" company="OpenTelemetry Authors">
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

#if NET6_0_OR_GREATER

using System.Reflection;
using OpenTelemetry.Instrumentation.AspNetCore;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class AspNetCoreMetricsInitializer : InstrumentationInitializer
{
    public AspNetCoreMetricsInitializer()
        : base("Microsoft.AspNetCore.Http")
    {
    }

    public override void Initialize(ILifespanManager lifespanManager)
    {
        var metricsType = Type.GetType("OpenTelemetry.Instrumentation.AspNetCore.AspNetCoreMetrics, OpenTelemetry.Instrumentation.AspNetCore")!;

        var aspNetCoreMetricsInstrumentationOptions = new AspNetCoreMetricsInstrumentationOptions();

        Instrumentation.PluginManager?.ConfigureMetricsOptions(aspNetCoreMetricsInstrumentationOptions);

        var constructor = metricsType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(AspNetCoreMetricsInstrumentationOptions) })!;
        var aspNetCoreMetrics = constructor.Invoke(new object?[] { aspNetCoreMetricsInstrumentationOptions });

        lifespanManager.Track(aspNetCoreMetrics);
    }
}
#endif
