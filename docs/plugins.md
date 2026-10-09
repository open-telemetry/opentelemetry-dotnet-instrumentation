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
The first configured plugin implementing `IOpAmpPlugin` controls OpAMP. Others
still participate through their other plugin interfaces. For file-based
configuration, `plugins` entries precede `plugins_list`; otherwise configured
list order applies.

> [!NOTE]
> The OpAMP client is owned by automatic instrumentation. Plugins receive a
> restricted client interface and cannot start, stop, or dispose the client.
> Unsupported capabilities are not advertised.

```csharp
using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Settings;

public class MyOpAmpPlugin : IPlugin, IOpAmpPlugin
{
    private IOpAmpClient? _client;

    public void Initializing()
    {
    }

    public void Initialized()
    {
    }

    public void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
        // Configure settings before client creation.
    }

    public void ConfigureOpAmpClient(IOpAmpClient client)
    {
        // Subscribe to messages here.
        _client = client;
    }

    public void AfterOpAmpClientStarted()
    {
        // Startup completed; connectivity is not guaranteed.
    }

    public void BeforeOpAmpClientStopped()
    {
        // Release resources and unsubscribe here.
    }
}
```

Initialization invokes `ConfigureOpAmpOptions`, `ConfigureOpAmpClient`, ordinary
`IPlugin.Initialized` callbacks, and client startup in that order. Subscribe in
`ConfigureOpAmpClient` to receive messages from the initial server response.
Client startup is asynchronous.

`AfterOpAmpClientStarted` means the client start operation completed; it does not
guarantee connectivity or initial-message delivery. It is skipped if startup
throws, is cancelled, or loses a race with shutdown. When invoked, it completes
before `BeforeOpAmpClientStopped` begins.

`BeforeOpAmpClientStopped` may run without `AfterOpAmpClientStarted`. Release
resources and unsubscribe there, including resources acquired before
`ConfigureOpAmpClient` throws. Lifecycle callbacks must return promptly and
tolerate client disposal during forced shutdown.

OpAMP requests do not produce application spans. Listener callbacks must return
promptly.

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
Its `ExportThreadSamples(byte[], int, uint, CancellationToken)` method receives
the CPU sampling interval in milliseconds for each native batch. Use that
interval to set the exported profile period. Existing exporters must update
their `ExportThreadSamples` implementation to accept the interval.

An exporter and a nonzero CPU interval or allocation limit prepare the
corresponding managed handler even when its `Enabled` setting is false. Native
sampling remains disabled until a runtime configuration enables that feature.

## Supported Options

### Tracing

| Options type                                                                              | NuGet package                                     | NuGet version |
|-------------------------------------------------------------------------------------------|---------------------------------------------------|---------------|
| OpenTelemetry.Exporter.ConsoleExporterOptions                                             | OpenTelemetry.Exporter.Console                    | 1.19.1        |
| OpenTelemetry.Exporter.ZipkinExporterOptions  **Deprecated**                              | OpenTelemetry.Exporter.Zipkin                     | 1.19.1        |
| OpenTelemetry.Exporter.OtlpExporterOptions                                                | OpenTelemetry.Exporter.OpenTelemetryProtocol      | 1.19.1        |
| OpenTelemetry.Instrumentation.AspNet.AspNetTraceInstrumentationOptions                    | OpenTelemetry.Instrumentation.AspNet              | 1.19.0        |
| OpenTelemetry.Instrumentation.AspNetCore.AspNetCoreTraceInstrumentationOptions            | OpenTelemetry.Instrumentation.AspNetCore          | 1.19.0        |
| OpenTelemetry.Instrumentation.EntityFrameworkCore.EntityFrameworkInstrumentationOptions   | OpenTelemetry.Instrumentation.EntityFrameworkCore | 1.19.0-beta.1 |
| OpenTelemetry.Instrumentation.GrpcNetClient.GrpcClientTraceInstrumentationOptions         | OpenTelemetry.Instrumentation.GrpcNetClient       | 1.19.1-beta.1 |
| OpenTelemetry.Instrumentation.Http.HttpClientTraceInstrumentationOptions                  | OpenTelemetry.Instrumentation.Http                | 1.19.0        |
| OpenTelemetry.Instrumentation.Quartz.QuartzInstrumentationOptions                         | OpenTelemetry.Instrumentation.Quartz              | 1.19.0-beta.1 |
| OpenTelemetry.Instrumentation.SqlClient.SqlClientTraceInstrumentationOptions              | OpenTelemetry.Instrumentation.SqlClient           | 1.19.0        |
| OpenTelemetry.Instrumentation.StackExchangeRedis.StackExchangeRedisInstrumentationOptions | OpenTelemetry.Instrumentation.StackExchangeRedis  | 1.19.0-beta.1 |
| OpenTelemetry.Instrumentation.Wcf.WcfInstrumentationOptions                               | OpenTelemetry.Instrumentation.Wcf                 | 1.19.1-beta.1 |

### Metrics

| Options type                                                             | NuGet package                                  | NuGet version |
|--------------------------------------------------------------------------|------------------------------------------------|---------------|
| OpenTelemetry.Metrics.MetricReaderOptions                                | OpenTelemetry                                  | 1.19.1        |
| OpenTelemetry.Exporter.ConsoleExporterOptions                            | OpenTelemetry.Exporter.Console                 | 1.19.1        |
| OpenTelemetry.Exporter.PrometheusExporterOptions                         | OpenTelemetry.Exporter.Prometheus.HttpListener | 1.19.1-beta.1 |
| OpenTelemetry.Exporter.OtlpExporterOptions                               | OpenTelemetry.Exporter.OpenTelemetryProtocol   | 1.19.1        |
| OpenTelemetry.Instrumentation.AspNet.AspNetMetricsInstrumentationOptions | OpenTelemetry.Instrumentation.AspNet           | 1.19.0        |
| OpenTelemetry.Instrumentation.Runtime.RuntimeInstrumentationOptions      | OpenTelemetry.Instrumentation.Runtime          | 1.19.0        |

### Logs

| Options type                                  | NuGet package                                | NuGet version |
|-----------------------------------------------|----------------------------------------------|---------------|
| OpenTelemetry.Logs.OpenTelemetryLoggerOptions | OpenTelemetry                                | 1.19.1        |
| OpenTelemetry.Exporter.ConsoleExporterOptions | OpenTelemetry.Exporter.Console               | 1.19.1        |
| OpenTelemetry.Exporter.OtlpExporterOptions    | OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.19.1        |

### OpAMP

| Settings type                                           | NuGet package              | NuGet version |
|---------------------------------------------------------|----------------------------|---------------|
| OpenTelemetry.OpAmp.Client.Settings.OpAmpClientSettings | OpenTelemetry.OpAmp.Client | 0.7.0-alpha.2 |

## Requirements

* The plugin must use the same `OpenTelemetry.AutoInstrumentation.PluginApi`
  version as OpenTelemetry .NET Automatic Instrumentation.
* The plugin must use the same `OpenTelemetry` version as OpenTelemetry .NET
  Automatic Instrumentation.
* The plugin must use the same options versions as OpenTelemetry .NET Automatic
  Instrumentation (found in the table above).
