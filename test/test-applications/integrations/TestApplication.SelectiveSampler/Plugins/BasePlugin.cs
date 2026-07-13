// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.Telemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace TestApplication.SelectiveSampler.Plugins;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
public class BasePlugin : IPlugin, ITelemetryPlugin
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
    public virtual MeterProviderBuilder AfterConfigureMeterProvider(MeterProviderBuilder builder)
    {
        return builder;
    }

    public virtual TracerProviderBuilder AfterConfigureTracerProvider(TracerProviderBuilder builder)
    {
        return builder;
    }

    public virtual MeterProviderBuilder BeforeConfigureMeterProvider(MeterProviderBuilder builder)
    {
        return builder;
    }

    public virtual TracerProviderBuilder BeforeConfigureTracerProvider(TracerProviderBuilder builder)
    {
        return builder;
    }

    public virtual ResourceBuilder ConfigureResource(ResourceBuilder builder)
    {
        return builder;
    }

    public virtual void Initialized()
    {
    }

    public virtual void Initializing()
    {
    }

    public virtual void MeterProviderInitialized(MeterProvider meterProvider)
    {
    }

    public virtual void TracerProviderInitialized(TracerProvider tracerProvider)
    {
    }
}
