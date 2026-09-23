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
Only the first configured plugin implementing `IOpAmpPlugin` controls OpAMP.
Additional OpAMP plugins are ignored for OpAMP and named in a warning, but they
continue to receive ordinary `IPlugin` callbacks and participate through any
other plugin interfaces they implement. For file-based configuration, entries
in `plugins` precede entries in `plugins_list`; otherwise list order is used.

> [!NOTE]
> The bundled `OpenTelemetry.OpAmp.Client` queues outgoing messages. The
> `Send*Async` methods available in 0.6.0-alpha.1 were replaced by corresponding
> `Send*` methods. Call `FlushAsync` when the plugin must wait until the outgoing
> queue is empty.

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
        // Called before the OpAMP client is created. Provider-backed reporting
        // must be explicitly enabled here when supported by this plugin.
    }

    public void ConfigureOpAmpClient(IOpAmpClient client)
    {
        // Called after client construction and before transport startup.
        // Register message listeners here and retain the client if needed later.
        _client = client;
    }

    public void AfterOpAmpClientStarted()
    {
        // Called after the OpAMP transport starts successfully.
    }

    public void BeforeOpAmpClientStopped()
    {
        // Called before the OpAMP client is stopped.
        // Avoid long-running work during application shutdown.
    }
}
```

OpAMP initialization calls `ConfigureOpAmpOptions`, constructs the client and
registers its internal listeners, calls `ConfigureOpAmpClient` on the selected
OpAMP plugin, and then invokes the ordinary `IPlugin.Initialized` callbacks. The
transport starts only after those callbacks complete. A listener subscribed in
`ConfigureOpAmpClient` therefore observes messages in the initial server
response. `AfterOpAmpClientStarted` runs only after successful startup. When
startup activation races with shutdown, the callback runs only if startup wins
the lifecycle transition, and it completes before `BeforeOpAmpClientStopped`
begins. `BeforeOpAmpClientStopped` may still run for a successfully prepared
client when startup fails, is cancelled, or loses that lifecycle transition, so
cleanup must not assume the post-start callback ran. Use
`IOpAmpClient.Unsubscribe` to remove a listener when it is no longer needed.

Implement `IProvideEffectiveConfig` to report effective configuration and
`IProvideRemoteConfigStatus` to report the status of remote configuration.
These interfaces make the corresponding reporting capabilities available to
the selected OpAMP plugin, but do not enable them. Both reporting settings are
disabled before `ConfigureOpAmpOptions` runs. The plugin must explicitly set
`EffectiveConfigurationReporting.EnableReporting` or
`RemoteConfiguration.ReportsRemoteConfigStatus` to `true`, optionally based on
its own local configuration. After the callback, a setting without its provider
is forced off, and the final choices remain fixed for the client lifetime.
Provider interfaces implemented only by ignored OpAMP plugins do not make
reporting available.
`RemoteConfiguration.AcceptsRemoteConfig` remains plugin-controlled.

`IProvideEffectiveConfig.GetEffectiveConfig` returns the plugin's complete
current effective configuration. Automatic instrumentation requests it when a
server starts accepting effective configuration and after the plugin calls
`IOpAmpClient.NotifyEffectiveConfigChanged` while that support is available.
Files are copied, sorted by name, and compared with the last valid state, so
equivalent state is not reported again. Effective configuration must satisfy
all of these limits:

- No more than 16 files.
- No file content larger than 512 KiB.
- File names must be ordinally unique.
- The empty string is a valid file name, including in a multi-file configuration.

`IProvideRemoteConfigStatus.GetRemoteConfigStatus` is requested when a server
starts offering remote configuration and after the plugin calls
`IOpAmpClient.NotifyRemoteConfigStatusChanged` while that support is available.
Return `null` when no remote configuration has been processed. Status reports
are also copied and sent only when their state changes. Notifications received
while server support is unavailable do not query providers; the current state
is requested if support later becomes available. If a provider throws, or an
effective configuration is invalid, the last valid state is retained and the
failure is logged without logging configuration contents. A failed refresh does
not send a partial report.

A server full-state request refreshes enabled providers supported by the server
and reports their latest valid state even when it has not changed. If a refresh
fails, the last valid state is used. Full-state requests are honored when
`ReportFullState` is combined with other server flags. Call
`IOpAmpClient.FlushAsync` to wait for state changes already accepted by
automatic instrumentation and for the upstream client's outgoing queue. Its
cancellation token and upstream failures are propagated to the caller. Call it
only after `AfterOpAmpClientStarted`; it fails if startup has not completed
successfully or shutdown has begun.

Call `IOpAmpClient.ReportCustomCapabilities` with the complete set of custom
capabilities supported by the plugin. Capability names are case-sensitive
reverse fully qualified domain names with optional version information. Each
call replaces the previous set; duplicate entries and ordering are ignored, and
an empty collection clears all previously reported capabilities. A custom
message is sent only when both the plugin and the server advertise its
capability. Custom capabilities may be reported during `ConfigureOpAmpClient`.
Before startup completes, only the latest set is retained and a nonempty set is
reported after the transport starts successfully. All later state reports are
serialized so a full-state response cannot overwrite a newer custom-capability
set. Pending replacement-state updates may be coalesced to their latest value.
Custom messages are accepted only for capabilities already submitted to
the upstream client. If capability publication is pending, await `FlushAsync`
and retry the message; a rejected message is not retried automatically. In
particular, a response produced by an initial-response listener may need to be
deferred until startup has completed and the capability report has been
flushed. Custom messages continue to use the upstream client's bounded queue.

Automatic instrumentation suppresses tracing around calls into the upstream
OpAMP client, including sends and flushes, so OpAMP transport requests do not
produce application spans. Provider callbacks do not modify suppression state;
they inherit the ambient state and should not depend on emitting telemetry.

### Security considerations

Automatic instrumentation does not validate plugin-defined remote
configuration or custom-message payloads, or redact reported effective
configuration. `RemoteConfiguration.AcceptsRemoteConfig` defaults to disabled
and is changed only by the selected plugin. Configure transport authentication
through `OpAmpClientSettings` and follow the
[OpAMP security recommendations](https://github.com/open-telemetry/opamp-spec/blob/v0.20.0/specification.md#security).

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
