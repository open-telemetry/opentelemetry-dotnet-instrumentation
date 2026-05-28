# Plugins

**Status**:
[Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md).

Plugins extend OpenTelemetry .NET Automatic Instrumentation by customizing SDK
setup, signal options, OpAMP, selective sampling, and continuous profiling.

Set `OTEL_DOTNET_AUTO_PLUGINS` to a colon-separated list of plugin type names,
specified with the
[assembly-qualified name](https://docs.microsoft.com/en-us/dotnet/api/system.type.assemblyqualifiedname?view=net-6.0#system-type-assemblyqualifiedname).
This list is colon-separated because type names can contain commas.

Every plugin type is instantiated at most once. A plugin must:

* reference the `OpenTelemetry.AutoInstrumentation.PluginApi` package version
  that matches the OpenTelemetry .NET Automatic Instrumentation version in use;
* be a concrete class with a public parameterless constructor;
* implement `OpenTelemetry.AutoInstrumentation.PluginApi.IPlugin`;
* optionally implement extension interfaces for additional capabilities.

Convention-based plugin methods are no longer discovered. Implementing `IPlugin`
is required for a plugin to function.

## Core lifecycle

All plugins implement `IPlugin`. The two lifecycle methods can use empty
implementations if the plugin only uses other extension points. These lifecycle
methods are called on every configured plugin.

```csharp
using OpenTelemetry.AutoInstrumentation.PluginApi;

public class MyPlugin : IPlugin
{
    public void Initializing()
    {
        // Called when auto instrumentation setup begins.
    }

    public void Initialized()
    {
        // Called after auto instrumentation setup is finalized.
    }
}
```

## Telemetry SDK customization

Implement `ITelemetryPlugin` to customize tracer and meter provider builders,
access built providers, or customize the resource builder.

```csharp
using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.Telemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

public class MyTelemetryPlugin : IPlugin, ITelemetryPlugin
{
    public void Initializing()
    {
    }

    public void Initialized()
    {
    }

    public TracerProviderBuilder BeforeConfigureTracerProvider(TracerProviderBuilder builder)
    {
        // Called before automatic instrumentation configures tracing.
        return builder;
    }

    public TracerProviderBuilder AfterConfigureTracerProvider(TracerProviderBuilder builder)
    {
        // Called after automatic instrumentation configures tracing, before Build().
        return builder;
    }

    public void TracerProviderInitialized(TracerProvider tracerProvider)
    {
        // Called after TracerProviderBuilder.Build().
    }

    public MeterProviderBuilder BeforeConfigureMeterProvider(MeterProviderBuilder builder)
    {
        // Called before automatic instrumentation configures metrics.
        return builder;
    }

    public MeterProviderBuilder AfterConfigureMeterProvider(MeterProviderBuilder builder)
    {
        // Called after automatic instrumentation configures metrics, before Build().
        return builder;
    }

    public void MeterProviderInitialized(MeterProvider meterProvider)
    {
        // Called after MeterProviderBuilder.Build().
    }

    public ResourceBuilder ConfigureResource(ResourceBuilder builder)
    {
        // Common resource customization for traces, metrics, and logs.
        return builder;
    }
}
```

`BeforeConfigureTracerProvider`, `AfterConfigureTracerProvider`,
`BeforeConfigureMeterProvider`, `AfterConfigureMeterProvider`, and
`ConfigureResource` return the builder that automatic instrumentation continues
to use. Only the first configured plugin implementing `ITelemetryPlugin` is used
for these builder-returning and resource-returning hooks.

`TracerProviderInitialized` and `MeterProviderInitialized` are called on every
configured plugin implementing `ITelemetryPlugin`.

## Signal options customization

Implement the generic options interfaces for each supported options type that
the plugin needs to configure:

* `IConfigureTracesOptions<TOptions>`
* `IConfigureMetricsOptions<TOptions>`
* `IConfigureLogsOptions<TOptions>`

The generic `TOptions` type must match one of the supported options types listed
below. All configured plugins implementing a matching options interface are
called in configuration order.

```csharp
using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.Telemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Logs;

public class MyOptionsPlugin : IPlugin,
    IConfigureTracesOptions<HttpClientTraceInstrumentationOptions>,
    IConfigureMetricsOptions<OtlpExporterOptions>,
    IConfigureLogsOptions<OpenTelemetryLoggerOptions>
{
    public void Initializing()
    {
    }

    public void Initialized()
    {
    }

    public void ConfigureTracesOptions(HttpClientTraceInstrumentationOptions options)
    {
        // Configure HTTP client trace instrumentation options.
    }

    public void ConfigureMetricsOptions(OtlpExporterOptions options)
    {
        // Configure OTLP metrics exporter options.
    }

    public void ConfigureLogsOptions(OpenTelemetryLoggerOptions options)
    {
        // Configure OpenTelemetry logger options.
    }
}
```

> [!NOTE]
> Automatic Instrumentation can configure particular properties before calling
> `Configure{Signal}Options`. It is the plugin's responsibility to not override
> this behavior.
> Example:
> `OpenTelemetry.Instrumentation.Http.HttpClientTraceInstrumentationOptions.EnrichWithHttpWebRequest`
> is conditionally set by this project.

## OpAMP

Implement `IOpAmpPlugin` to customize the OpAMP client and observe its
lifecycle.
OpAMP methods are called on every configured plugin implementing `IOpAmpPlugin`.

```csharp
using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client;
using OpenTelemetry.OpAmp.Client.Settings;

public class MyOpAmpPlugin : IPlugin, IOpAmpPlugin
{
    public void Initializing()
    {
    }

    public void Initialized()
    {
    }

    public void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
        // Called before the OpAMP client is created.
    }

    public void AfterOpAmpClientStarted(OpAmpClient client)
    {
        // Called after the OpAMP client is created and started.
    }

    public void BeforeOpAmpClientStopped()
    {
        // Called before the OpAMP client is stopped.
        // Avoid long-running work during application shutdown.
    }
}
```

## Selective sampling

Implement `ISelectiveSamplerPlugin` to provide selective sampling configuration.
Only the first configured plugin implementing `ISelectiveSamplerPlugin` is used.

```csharp
using System;
using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.SelectiveSampling;

public class MySelectiveSamplerPlugin : IPlugin, ISelectiveSamplerPlugin
{
    public void Initializing()
    {
    }

    public void Initialized()
    {
    }

    public SelectiveSamplerConfiguration? GetFirstSelectiveSamplingConfiguration()
    {
        return new SelectiveSamplerConfiguration
        {
            SamplingInterval = 200,
            ExportInterval = TimeSpan.FromMilliseconds(50),
            ExportTimeout = TimeSpan.FromSeconds(5),
            Exporter = new MySelectiveSamplerExporter()
        };
    }
}
```

`Exporter` must implement `ISelectiveSamplerExporter`.

## Continuous profiling

Implement `IContinuousProfilerPlugin` to provide continuous profiler
configuration. Only the first configured plugin implementing
`IContinuousProfilerPlugin` is used. If no plugin provides a configuration, the
default continuous profiler configuration is used.

```csharp
using System;
using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.ContinuousProfiling;

public class MyContinuousProfilerPlugin : IPlugin, IContinuousProfilerPlugin
{
    public void Initializing()
    {
    }

    public void Initialized()
    {
    }

    public ContinuousProfilerConfiguration GetFirstContinuousProfilerConfiguration()
    {
        return new ContinuousProfilerConfiguration
        {
            ThreadSamplingEnabled = true,
            ThreadSamplingInterval = 1000,
            AllocationSamplingEnabled = false,
            MaxMemorySamplesPerMinute = 200,
            ExportInterval = TimeSpan.FromMilliseconds(500),
            ExportTimeout = TimeSpan.FromSeconds(5),
            Exporter = new MyContinuousProfilerExporter()
        };
    }
}
```

`Exporter` must implement `IContinuousProfilerExporter`.

## Supported Options

### Tracing

| Options type                                                                              | NuGet package                                     | NuGet version |
|-------------------------------------------------------------------------------------------|---------------------------------------------------|---------------|
| OpenTelemetry.Exporter.ConsoleExporterOptions                                             | OpenTelemetry.Exporter.Console                    | 1.15.3        |
| OpenTelemetry.Exporter.ZipkinExporterOptions  **Deprecated**                              | OpenTelemetry.Exporter.Zipkin                     | 1.15.3        |
| OpenTelemetry.Exporter.OtlpExporterOptions                                                | OpenTelemetry.Exporter.OpenTelemetryProtocol      | 1.15.3        |
| OpenTelemetry.Instrumentation.AspNet.AspNetTraceInstrumentationOptions                    | OpenTelemetry.Instrumentation.AspNet              | 1.15.2        |
| OpenTelemetry.Instrumentation.AspNetCore.AspNetCoreTraceInstrumentationOptions            | OpenTelemetry.Instrumentation.AspNetCore          | 1.15.2        |
| OpenTelemetry.Instrumentation.EntityFrameworkCore.EntityFrameworkInstrumentationOptions   | OpenTelemetry.Instrumentation.EntityFrameworkCore | 1.15.1-beta.1 |
| OpenTelemetry.Instrumentation.GrpcNetClient.GrpcClientTraceInstrumentationOptions         | OpenTelemetry.Instrumentation.GrpcNetClient       | 1.15.1-beta.1 |
| OpenTelemetry.Instrumentation.Http.HttpClientTraceInstrumentationOptions                  | OpenTelemetry.Instrumentation.Http                | 1.15.1        |
| OpenTelemetry.Instrumentation.Quartz.QuartzInstrumentationOptions                         | OpenTelemetry.Instrumentation.Quartz              | 1.15.1-beta.1 |
| OpenTelemetry.Instrumentation.SqlClient.SqlClientTraceInstrumentationOptions              | OpenTelemetry.Instrumentation.SqlClient           | 1.15.2        |
| OpenTelemetry.Instrumentation.StackExchangeRedis.StackExchangeRedisInstrumentationOptions | OpenTelemetry.Instrumentation.StackExchangeRedis  | 1.15.1-beta.1 |
| OpenTelemetry.Instrumentation.Wcf.WcfInstrumentationOptions                               | OpenTelemetry.Instrumentation.Wcf                 | 1.15.1-beta.2 |

### Metrics

| Options type                                                             | NuGet package                                  | NuGet version |
|--------------------------------------------------------------------------|------------------------------------------------|---------------|
| OpenTelemetry.Metrics.MetricReaderOptions                                | OpenTelemetry                                  | 1.15.3        |
| OpenTelemetry.Exporter.ConsoleExporterOptions                            | OpenTelemetry.Exporter.Console                 | 1.15.3        |
| OpenTelemetry.Exporter.PrometheusExporterOptions                         | OpenTelemetry.Exporter.Prometheus.HttpListener | 1.15.3-beta.1 |
| OpenTelemetry.Exporter.OtlpExporterOptions                               | OpenTelemetry.Exporter.OpenTelemetryProtocol   | 1.15.3        |
| OpenTelemetry.Instrumentation.AspNet.AspNetMetricsInstrumentationOptions | OpenTelemetry.Instrumentation.AspNet           | 1.15.2        |
| OpenTelemetry.Instrumentation.Runtime.RuntimeInstrumentationOptions      | OpenTelemetry.Instrumentation.Runtime          | 1.15.1        |

### Logs

| Options type                                  | NuGet package                                | NuGet version |
|-----------------------------------------------|----------------------------------------------|---------------|
| OpenTelemetry.Logs.OpenTelemetryLoggerOptions | OpenTelemetry                                | 1.15.3        |
| OpenTelemetry.Exporter.ConsoleExporterOptions | OpenTelemetry.Exporter.Console               | 1.15.3        |
| OpenTelemetry.Exporter.OtlpExporterOptions    | OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.15.3        |

### OpAMP

| Settings type                                           | NuGet package              | NuGet version |
|---------------------------------------------------------|----------------------------|---------------|
| OpenTelemetry.OpAmp.Client.Settings.OpAmpClientSettings | OpenTelemetry.OpAmp.Client | 0.4.0-alpha.1 |

## Requirements

* The plugin must use the same `OpenTelemetry.AutoInstrumentation.PluginApi`
  version as OpenTelemetry .NET Automatic Instrumentation.
* The plugin must use the same `OpenTelemetry` version as OpenTelemetry .NET
  Automatic Instrumentation.
* The plugin must use the same options versions as OpenTelemetry .NET Automatic
  Instrumentation (found in the table above).
