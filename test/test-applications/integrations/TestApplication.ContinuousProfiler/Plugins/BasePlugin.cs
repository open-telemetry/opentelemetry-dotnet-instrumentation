// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.Telemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace TestApplication.ContinuousProfiler.Plugins;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
public class BasePlugin : IPlugin, ITelemetryPlugin
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
    public MeterProviderBuilder AfterConfigureMeterProvider(MeterProviderBuilder builder)
    {
        return builder;
    }

    public TracerProviderBuilder AfterConfigureTracerProvider(TracerProviderBuilder builder)
    {
        return builder;
    }

    public MeterProviderBuilder BeforeConfigureMeterProvider(MeterProviderBuilder builder)
    {
        return builder;
    }

    public TracerProviderBuilder BeforeConfigureTracerProvider(TracerProviderBuilder builder)
    {
        return builder;
    }

    public virtual ResourceBuilder ConfigureResource(ResourceBuilder builder)
    {
        return builder;
    }

    public void Initialized()
    {
    }

    public void Initializing()
    {
    }

    public void MeterProviderInitialized(MeterProvider meterProvider)
    {
    }

    public void TracerProviderInitialized(TracerProvider tracerProvider)
    {
    }
}
