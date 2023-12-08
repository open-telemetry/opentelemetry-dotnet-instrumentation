// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER

using System.Reflection;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class AspNetCoreMetricsInitializer : InstrumentationInitializer
{
    public AspNetCoreMetricsInitializer()
        : base("Microsoft.AspNetCore.Http")
    {
    }

    public override void Initialize(ILifespanManager lifespanManager)
    {
        var metricOptionsType = Type.GetType("OpenTelemetry.Instrumentation.AspNetCore.AspNetCoreMetricsInstrumentationOptions, OpenTelemetry.Instrumentation.AspNetCore")!;
        var optionsConstructor = metricOptionsType.GetConstructor(Type.EmptyTypes)!;
        var aspNetCoreMetricsInstrumentationOptions = optionsConstructor.Invoke(Array.Empty<object?>());

        var metricsType = Type.GetType("OpenTelemetry.Instrumentation.AspNetCore.AspNetCoreMetrics, OpenTelemetry.Instrumentation.AspNetCore")!;
        var constructor = metricsType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, new[] { metricOptionsType })!;
        var aspNetCoreMetrics = constructor.Invoke(new object?[] { aspNetCoreMetricsInstrumentationOptions });

        lifespanManager.Track(aspNetCoreMetrics);
    }
}
#endif
