# Configuration

## Configuration methods

You can apply configuration settings in the following ways,
with environment variables taking precedence over `App.config` or `Web.config` file:

1. Environment variables

    Environment variables are the main way to configure the settings.

2. `App.config` or `Web.config` file

    For an application running on .NET Framework, you can use a web configuration
    file  (`web.config`) or an application configuration file (`app.config`) to
    configure the `OTEL_*` settings.

    ⚠️ Only settings starting with `OTEL_` can be set using `App.config` or `Web.config`.
    However, the following settings are not supported:

    - `OTEL_DOTNET_AUTO_HOME`
    - `OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES`
    - `OTEL_DOTNET_AUTO_INTEGRATIONS_FILE`
    - `OTEL_DOTNET_AUTO_[TRACES|METRICS|LOGS]_[ENABLED|DISABLED]_INSTRUMENTATIONS`
    - `OTEL_DOTNET_AUTO_LOG_DIRECTORY`
    - `OTEL_LOG_LEVEL`
    - `OTEL_DOTNET_AUTO_NETFX_REDIRECT_ENABLED`

    Example with `OTEL_SERVICE_NAME` setting:

    ```xml
    <configuration>
    <appSettings>
        <add key="OTEL_SERVICE_NAME" value="my-service-name" />
    </appSettings>
    </configuration>
    ```

By default we recommend using environment variables for configuration.
However, if given setting supports it, then:

- use `Web.config` for configuring an ASP.NET application (.NET Framework),
- use `App.config` for configuring a Windows Service (.NET Framework).

## Global settings

| Environment variable                 | Description                                                                                                                                                                                                                  | Default value |
|--------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------|
| `OTEL_DOTNET_AUTO_HOME`              | Installation location.                                                                                                                                                                                                       |               |
| `OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES` | Names of the executable files that the profiler cannot instrument. Supports multiple comma-separated values, for example: `ReservedProcess.exe,powershell.exe`. If unset, the profiler attaches to all processes by default. |               |
| `OTEL_LOG_LEVEL`                     | SDK log level. (supported values: `none`,`error`,`warn`,`info`,`debug`)                                                                                                                                                      | `info`        |

## Resources

A resource is the immutable representation of the entity producing the telemetry.
See [Resource semantic conventions](https://github.com/open-telemetry/opentelemetry-specification/tree/main/specification/resource/semantic_conventions)
for more details.

| Environment variable       | Description                                                                                                                                                                                                                                                                                                       | Default value                                                                                                                                                                                                                       |
|----------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `OTEL_RESOURCE_ATTRIBUTES` | Key-value pairs to be used as resource attributes. See [Resource SDK](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/sdk.md#specifying-resource-information-via-an-environment-variable) for more details.                                                        | See [Resource semantic conventions](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/semantic_conventions/README.md#semantic-attributes-with-sdk-provided-default-value) for details. |
| `OTEL_SERVICE_NAME`        | Sets the value of the [`service.name`](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/semantic_conventions/README.md#service) resource attribute. If `service.name` is provided in `OTEL_RESOURCE_ATTRIBUTES`, the value of `OTEL_SERVICE_NAME` takes precedence. | `unknown_service:%ProcessName%`                                                                                                                                                                                                     |

## Instrumentations

All instrumentations are enabled by default for all signal types
(traces, metrics, and logs).

You can disable all instrumentations for a specific signal type by setting
the `OTEL_DOTNET_AUTO_{SIGNAL}_INSTRUMENTATION_ENABLED`
environment variable to `false`.

For a more granular approach, you can disable specific instrumentations
for a given signal type by setting
the `OTEL_DOTNET_AUTO_{SIGNAL}_{0}_INSTRUMENTATION_ENABLED`
environment variable to `false`, where `{SIGNAL}` is the type of signal,
for example `TRACES`, and `{0}` is the case-sensitive name of the instrumentation.

| Environment variable                                   | Description                                                                                                                                                                                                                                                     | Default value                                                                          |
|--------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------|
| `OTEL_DOTNET_AUTO_INTEGRATIONS_FILE`                   | List of bytecode instrumentations JSON configuration filepaths, delimited by the platform-specific path separator (`;` on Windows, `:` on Linux and macOS). For example: `%ProfilerDirectory%/integrations.json`. It is required for bytecode instrumentations. |                                                                                        |
| `OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED`             | Disables all instrumentations.                                                                                                                                                                                                                                  | `true`                                                                                 |
| `OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED`      | Disables all trace instrumentations. Overrides `OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED`.                                                                                                                                                                      | Inherited from the current value of `OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED`         |
| `OTEL_DOTNET_AUTO_TRACES_{0}_INSTRUMENTATION_ENABLED`  | Configuration pattern for enabling or disabling a specific trace instrumentation, where `{0}` is the uppercase id of the instrumentation you want to enable. Overrides `OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED`.                                       | Inherited from the current value of `OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED`  |
| `OTEL_DOTNET_AUTO_METRICS_INSTRUMENTATION_ENABLED`     | Disables all metric instrumentations. Overrides `OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED`.                                                                                                                                                                     | Inherited from the current value of `OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED`         |
| `OTEL_DOTNET_AUTO_METRICS_{0}_INSTRUMENTATION_ENABLED` | Configuration pattern for enabling or disabling a specific metric instrumentation, where `{0}` is the uppercase id of the instrumentation you want to enable. Overrides `OTEL_DOTNET_AUTO_METRICS_INSTRUMENTATION_ENABLED`.                                     | Inherited from the current value of `OTEL_DOTNET_AUTO_METRICS_INSTRUMENTATION_ENABLED` |
| `OTEL_DOTNET_AUTO_LOGS_INSTRUMENTATION_ENABLED`        | Disables all log instrumentations. Overrides `OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED`.                                                                                                                                                                        | Inherited from the current value of `OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED`         |
| `OTEL_DOTNET_AUTO_LOGS_{0}_INSTRUMENTATION_ENABLED`    | Configuration pattern for enabling or disabling a specific log instrumentation, where `{0}` is the uppercase id of the instrumentation you want to enable. Overrides `OTEL_DOTNET_AUTO_LOGS_INSTRUMENTATION_ENABLED`.                                           | Inherited from the current value of `OTEL_DOTNET_AUTO_LOGS_INSTRUMENTATION_ENABLED`    |

### Traces instrumentations

| ID                    | Instrumented library                                                                                                                                                                            | Supported versions | Instrumentation type    |
|-----------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------|-------------------------|
| `ASPNET`              | ASP.NET (.NET Framework) MVC / WebApi \[1\] **Not supported on .NET**                                                                                                                           | *                  | source                  |
| `ASPNETCORE`          | ASP.NET Core **Not supported on .NET Framework**                                                                                                                                                | *                  | source                  |
| `ELASTICSEARCH`       | [Elastic.Clients.Elasticsearch](https://www.nuget.org/packages/Elastic.Clients.Elasticsearch)                                                                                                   | ≥8.0.0             | source                  |
| `ENTITYFRAMEWORKCORE` | [Microsoft.EntityFrameworkCore](https://www.nuget.org/packages/) **Not supported on .NET Framework**                                                                                            | ≥6.0.12            | source                  |
| `GRAPHQL`             | [GraphQL](https://www.nuget.org/packages/GraphQL)                                                                                                                                               | ≥2.3.0 & < 3.0.0   | bytecode                |
| `GRPCNETCLIENT`       | [Grpc.Net.Client](https://www.nuget.org/packages/Grpc.Net.Client)                                                                                                                               | ≥2.43.0 & < 3.0.0  | source                  |
| `HTTPCLIENT`          | [System.Net.Http.HttpClient](https://docs.microsoft.com/dotnet/api/system.net.http.httpclient) and [System.Net.HttpWebRequest](https://docs.microsoft.com/dotnet/api/system.net.httpwebrequest) | *                  | source                  |
| `QUARTZ`              | [Quartz](https://www.nuget.org/packages/Quartz) **Not supported on .NET Framework 4.7.1 and older**                                                                                             | ≥3.4.0             | source                  |
| `MASSTRANSIT`         | [MassTransit](https://www.nuget.org/packages/MassTransit) **Not supported on .NET Framework**                                                                                                   | ≥8.0.0             | source                  |
| `MONGODB`             | [MongoDB.Driver.Core](https://www.nuget.org/packages/MongoDB.Driver.Core) **Not supported on .NET Framework**                                                                                   | ≥2.13.3 & < 3.0.0  | source & bytecode       |
| `MYSQLDATA`           | [MySql.Data](https://www.nuget.org/packages/MySql.Data) **Not supported on .NET Framework**                                                                                                     | ≥6.10.7            | source & bytecode \[2\] |
| `NPGSQL`              | [Npgsql](https://www.nuget.org/packages/Npgsql)                                                                                                                                                 | ≥6.0.0             | source                  |
| `NSERVICEBUS`         | [NServiceBus](https://www.nuget.org/packages/NServiceBus)                                                                                                                                       | ≥8.0.0             | source & bytecode       |
| `SQLCLIENT`           | [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient) and [System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient)                           | * \[3\]            | source                  |
| `STACKEXCHANGEREDIS`  | [StackExchange.Redis](https://www.nuget.org/packages/StackExchange.Redis) **Not supported on .NET Framework**                                                                                   | ≥2.0.405 < 3.0.0   | source & bytecode       |
| `WCF`                 | [System.ServiceModel](https://www.nuget.org/packages/System.ServiceModel) **No support for server side on .NET**. For configuration see [WCF Instrumentation Configuration](wcf-config.md)      | * \[4\]            | source                  |

\[1\]: Only integrated pipeline mode is supported.

\[2\]: MySql.Data 8.0.31 and higher requires bytecode instrumentation.

\[3\]: Microsoft.Data.SqlClient v3.* is not supported on .NET Framework, due to [issue](https://github.com/open-telemetry/opentelemetry-dotnet/issues/4243).

\[4\]: On .NET it supports [System.ServiceModel.Primitives](https://www.nuget.org/packages/System.ServiceModel.Primitives)
≥ 4.7.0.

### Metrics instrumentations

| ID            | Instrumented library                                                                                                                                                                            | Supported versions | Instrumentation type |
|---------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------|----------------------|
| `ASPNET`      | ASP.NET Framework \[1\] **Not supported on .NET**                                                                                                                                               | *                  | source               |
| `ASPNETCORE`  | ASP.NET Core \[2\]  **Not supported on .NET Framework**                                                                                                                                         | *                  | source               |
| `HTTPCLIENT`  | [System.Net.Http.HttpClient](https://docs.microsoft.com/dotnet/api/system.net.http.httpclient) and [System.Net.HttpWebRequest](https://docs.microsoft.com/dotnet/api/system.net.httpwebrequest) | *                  | source               |
| `NETRUNTIME`  | [OpenTelemetry.Instrumentation.Runtime](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Runtime)                                                                                   | *                  | source               |
| `PROCESS`     | [OpenTelemetry.Instrumentation.Process](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Process)                                                                                   | *                  | source               |
| `NSERVICEBUS` | [NServiceBus](https://www.nuget.org/packages/NServiceBus)                                                                                                                                       | ≥8.0.0             | source & bytecode    |

\[1\]: The ASP.NET metrics are generated only if the `AspNet` trace instrumentation
 is also enabled.

\[2\]: This instrumentation automatically enables the
 `Microsoft.AspNetCore.Hosting.HttpRequestIn` spans.

### Logs instrumentations

| ID      | Instrumented library                                                                                                            | Supported versions | Instrumentation type   |
|---------|---------------------------------------------------------------------------------------------------------------------------------|--------------------|------------------------|
| ILOGGER | [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging) **Not supported on .NET Framework** | ≥6.0.0             | bytecode or source [1] |

**[1]**: For ASP.NET Core applications, the `LoggingBuilder` instrumentation
can be enabled without using the .NET CLR Profiler by setting
the `ASPNETCORE_HOSTINGSTARTUPASSEMBLIES` environment variable to
`OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper`.

### Instrumentation options

| Environment variable                    | Description                                                                                                                                                        | Default value |
|-----------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------|
| `OTEL_DOTNET_AUTO_GRAPHQL_SET_DOCUMENT` | Whether GraphQL instrumentation can pass raw queries as `graphql.document` attribute. This may contain sensitive information and therefore is disabled by default. | `false`       |

## Propagators

Propagators allow applications to share context. See [the OpenTelemetry specification](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/context/api-propagators.md)
for more details.

| Environment variable | Description                                                                                                                                                                                                                                                                                                  | Default value          |
|----------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------|
| `OTEL_PROPAGATORS`   | Comma-separated list of propagators. Supported options: `tracecontext`, `baggage`, `b3multi`, `b3`. See [the OpenTelemetry specification](https://github.com/open-telemetry/opentelemetry-specification/blob/v1.14.0/specification/sdk-environment-variables.md#general-sdk-configuration) for more details. | `tracecontext,baggage` |

## Samplers

Samplers let you control potential noise and overhead introduced
by OpenTelemetry instrumentation by selecting which traces you want
to collect and export.
See [the OpenTelemetry specification](https://github.com/open-telemetry/opentelemetry-specification/blob/v1.15.0/specification/sdk-environment-variables.md?plain=1#L45-L80)
for more details.

| Environment variable      | Description                                           | Default value           |
|---------------------------|-------------------------------------------------------|-------------------------|
| `OTEL_TRACES_SAMPLER`     | Sampler to be used for traces \[1\]                   | `parentbased_always_on` |
| `OTEL_TRACES_SAMPLER_ARG` | String value to be used as the sampler argument \[2\] |                         |

\[1\]: Supported values are:

- `always_on`,
- `always_off`,
- `traceidratio`,
- `parentbased_always_on`,
- `parentbased_always_off`,
- `parentbased_traceidratio`.

\[2\]: For `traceidratio` and `parentbased_traceidratio` samplers:
 Sampling probability, a number in the [0..1] range, e.g. "0.25".
 Default is 1.0.

## Exporters

Exporters output the telemetry.

| Environment variable    | Description                                                                                       | Default value |
|-------------------------|---------------------------------------------------------------------------------------------------|---------------|
| `OTEL_TRACES_EXPORTER`  | Traces exporter to be used. The value can be one of the following: `zipkin`, `otlp`, `none`.      | `otlp`        |
| `OTEL_METRICS_EXPORTER` | Metrics exporter to be used. The value can be one of the following: `otlp`, `prometheus`, `none`. | `otlp`        |
| `OTEL_LOGS_EXPORTER`    | Logs exporter to be used. The value can be one of the following: `otlp`, `none`.                  | `otlp`        |

### Traces exporter

| Environment variable             | Description                                                                  | Default value |
|----------------------------------|------------------------------------------------------------------------------|---------------|
| `OTEL_BSP_SCHEDULE_DELAY`        | Delay interval (in milliseconds) between two consecutive exports.            | `5000`        |
| `OTEL_BSP_EXPORT_TIMEOUT`        | Maximum allowed time (in milliseconds) to export data                        | `30000`       |
| `OTEL_BSP_MAX_QUEUE_SIZE`        | Maximum queue size.                                                          | `2048`        |
| `OTEL_BSP_MAX_EXPORT_BATCH_SIZE` | Maximum batch size. Must be less than or equal to `OTEL_BSP_MAX_QUEUE_SIZE`. | `512`         |

### Metrics exporter

| Environment variable          | Description                                                                   | Default value                                           |
|-------------------------------|-------------------------------------------------------------------------------|---------------------------------------------------------|
| `OTEL_METRIC_EXPORT_INTERVAL` | The time interval (in milliseconds) between the start of two export attempts. | `60000` for OTLP exporter, `10000` for console exporter |
| `OTEL_METRIC_EXPORT_TIMEOUT`  | Maximum allowed time (in milliseconds) to export data.                        | `30000` for OTLP exporter, none for console exporter    |

### Logs exporter

| Environment variable                              | Description                                             | Default value |
|---------------------------------------------------|---------------------------------------------------------|---------------|
| `OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE` | Whether the formatted log message should be set or not. | `false`       |

### OTLP

To enable the OTLP exporter, set the `OTEL_TRACES_EXPORTER`/`OTEL_METRICS_EXPORTER`/`OTEL_LOGS_EXPORTER`
environment variable to `otlp`.

To customize the OTLP exporter using environment variables, see the
[OTLP exporter documentation](https://github.com/open-telemetry/opentelemetry-dotnet/tree/core-1.4.0-rc.3/src/OpenTelemetry.Exporter.OpenTelemetryProtocol#environment-variables).
Important environment variables include:

| Environment variable                     | Description                                                                                                                                                                                                | Default value                                                                                             |
|------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------|
| `OTEL_EXPORTER_OTLP_ENDPOINT`            | Target endpoint for the OTLP exporter. See [the OpenTelemetry specification](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/exporter.md) for more details. | `http://localhost:4318` for the `http/protobuf` protocol, `http://localhost:4317` for the `grpc` protocol |
| `OTEL_EXPORTER_OTLP_PROTOCOL`            | OTLP exporter transport protocol. Supported values are `grpc`, `http/protobuf`. [1]                                                                                                                        | `http/protobuf`                                                                                           |
| `OTEL_EXPORTER_OTLP_TIMEOUT`             | The max waiting time (in milliseconds) for the backend to process each batch.                                                                                                                              | `10000`                                                                                                   |
| `OTEL_EXPORTER_OTLP_HEADERS`             | Comma-separated list of additional HTTP headers sent with each export, for example: `Authorization=secret,X-Key=Value`.                                                                                    |                                                                                                           |
| `OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT`      | Maximum allowed attribute value size.                                                                                                                                                                      | none                                                                                                      |
| `OTEL_ATTRIBUTE_COUNT_LIMIT`             | Maximum allowed span attribute count.                                                                                                                                                                      | 128                                                                                                       |
| `OTEL_SPAN_ATTRIBUTE_VALUE_LENGTH_LIMIT` | Maximum allowed attribute value size. [Not applicable for metrics.](https://github.com/open-telemetry/opentelemetry-specification/blob/v1.15.0/specification/metrics/sdk.md#attribute-limits).             | none                                                                                                      |
| `OTEL_SPAN_ATTRIBUTE_COUNT_LIMIT`        | Maximum allowed span attribute count. [Not applicable for metrics.](https://github.com/open-telemetry/opentelemetry-specification/blob/v1.15.0/specification/metrics/sdk.md#attribute-limits).             | 128                                                                                                       |
| `OTEL_SPAN_EVENT_COUNT_LIMIT`            | Maximum allowed span event count.                                                                                                                                                                          | 128                                                                                                       |
| `OTEL_SPAN_LINK_COUNT_LIMIT`             | Maximum allowed span link count.                                                                                                                                                                           | 128                                                                                                       |
| `OTEL_EVENT_ATTRIBUTE_COUNT_LIMIT`       | Maximum allowed attribute per span event count.                                                                                                                                                            | 128                                                                                                       |
| `OTEL_LINK_ATTRIBUTE_COUNT_LIMIT`        | Maximum allowed attribute per span link count.                                                                                                                                                             | 128                                                                                                       |

**[1]**: Considerations on the `OTEL_EXPORTER_OTLP_PROTOCOL`:

- The OpenTelemetry .NET Automatic Instrumentation defaults to `http/protobuf`,
  which differs from the OpenTelemetry .NET SDK default value of `grpc`.
- On .NET 6 and higher, the application must reference [`Grpc.Net.Client`](https://www.nuget.org/packages/Grpc.Net.Client/)
  to use the `grpc` OTLP exporter protocol. For example, by adding
  `<PackageReference Include="Grpc.Net.Client" Version="2.43.0" />` to the
  `.csproj` file.
- On .NET Framework, the `grpc` OTLP exporter protocol is not supported.

### Prometheus

> **Warning**
> **Do NOT use in production.**
>
> Prometheus exporter is intended for the inner dev loop.
> Production environments can use a combination of OTLP exporter
> with [OpenTelemetry Collector](https://github.com/open-telemetry/opentelemetry-collector-releases)
> having [`otlp` receiver](https://github.com/open-telemetry/opentelemetry-collector/tree/v0.61.0/receiver/otlpreceiver)
> and [`prometheus` exporter](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/v0.61.0/exporter/prometheusexporter).

To enable the Prometheus exporter, set the `OTEL_METRICS_EXPORTER` environment
variable to `prometheus`.

The exporter exposes the metrics HTTP endpoint on `http://localhost:9464/metrics`
and it caches the responses for 300 milliseconds.

See the
[Prometheus Exporter HttpListener documentation](https://github.com/open-telemetry/opentelemetry-dotnet/tree/core-1.4.0-rc.3/src/OpenTelemetry.Exporter.Prometheus.HttpListener).
to learn more.

### Zipkin

To enable the Zipkin exporter, set the `OTEL_TRACES_EXPORTER` environment
variable to `zipkin`.

To customize the Zipkin exporter using environment variables,
see the [Zipkin exporter documentation](https://github.com/open-telemetry/opentelemetry-dotnet/tree/core-1.4.0-rc.3/src/OpenTelemetry.Exporter.Zipkin#configuration-using-environment-variables).
Important environment variables include:

| Environment variable            | Description | Default value                        |
|---------------------------------|-------------|--------------------------------------|
| `OTEL_EXPORTER_ZIPKIN_ENDPOINT` | Zipkin URL  | `http://localhost:9411/api/v2/spans` |

## Additional settings

| Environment variable                                | Description                                                                                                                                                                                                                                                                                                                                                                                        | Default value |
|-----------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------|
| `OTEL_DOTNET_AUTO_TRACES_ENABLED`                   | Enables traces.                                                                                                                                                                                                                                                                                                                                                                                    | `true`        |
| `OTEL_DOTNET_AUTO_OPENTRACING_ENABLED`              | Enables OpenTracing tracer.                                                                                                                                                                                                                                                                                                                                                                        | `false`       |
| `OTEL_DOTNET_AUTO_LOGS_ENABLED`                     | Enables logs.                                                                                                                                                                                                                                                                                                                                                                                      | `true`        |
| `OTEL_DOTNET_AUTO_METRICS_ENABLED`                  | Enables metrics.                                                                                                                                                                                                                                                                                                                                                                                   | `true`        |
| `OTEL_DOTNET_AUTO_NETFX_REDIRECT_ENABLED`           | Enables automatic redirection of the assemblies used by the automatic instrumentation on the .NET Framework.                                                                                                                                                                                                                                                                                       | `true`        |
| `OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES`        | Comma-separated list of additional `System.Diagnostics.ActivitySource` names to be added to the tracer at the startup. Use it to capture manually instrumented spans.                                                                                                                                                                                                                              |               |
| `OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_LEGACY_SOURCES` | Comma-separated list of additional legacy source names to be added to the tracer at the startup. Use it to capture `System.Diagnostics.Activity` objects created without using the `System.Diagnostics.ActivitySource` API.                                                                                                                                                                        |               |
| `OTEL_DOTNET_AUTO_FLUSH_ON_UNHANDLEDEXCEPTION`      | Controls whether the telemetry data is flushed when an [AppDomain.UnhandledException](https://docs.microsoft.com/en-us/dotnet/api/system.appdomain.unhandledexception) event is raised. Set to `true` when you suspect that you are experiencing a problem with missing telemetry data and also experiencing unhandled exceptions.                                                                 | `false`       |
| `OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES`       | Comma-separated list of additional `System.Diagnostics.Metrics.Meter` names to be added to the meter at the startup. Use it to capture manually instrumented spans.                                                                                                                                                                                                                                |               |
| `OTEL_DOTNET_AUTO_PLUGINS`                          | Colon-separated list of OTel SDK instrumentation plugin types, specified with the [assembly-qualified name](https://docs.microsoft.com/en-us/dotnet/api/system.type.assemblyqualifiedname?view=net-6.0#system-type-assemblyqualifiedname). _Note: This list must be colon-separated because the type names may include commas._ See more info on how to write plugins at [plugins.md](plugins.md). |               |

## .NET CLR Profiler

The CLR uses the following
environment variables to set up the profiler. See
[.NET Runtime Profiler Loading](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/profiling/Profiler%20Loading.md)
for more information.

| .NET Framework environment variable | .NET environment variable  | Description                                                                             | Required value                                                                                                                                                                                                                                                  |
|-------------------------------------|----------------------------|-----------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `COR_ENABLE_PROFILING`              | `CORECLR_ENABLE_PROFILING` | Enables the profiler.                                                                   | `1`                                                                                                                                                                                                                                                             |
| `COR_PROFILER`                      | `CORECLR_PROFILER`         | CLSID of the profiler.                                                                  | `{918728DD-259F-4A6A-AC2B-B85E1B658318}`                                                                                                                                                                                                                        |
| `COR_PROFILER_PATH`                 | `CORECLR_PROFILER_PATH`    | Path to the profiler.                                                                   | `$INSTALL_DIR/linux-x64/OpenTelemetry.AutoInstrumentation.Native.so` for Linux glibc, `$INSTALL_DIR/linux-musl-x64/OpenTelemetry.AutoInstrumentation.Native.so` for Linux musl, `$INSTALL_DIR/osx-x64/OpenTelemetry.AutoInstrumentation.Native.dylib` for macOS |
| `COR_PROFILER_PATH_32`              | `CORECLR_PROFILER_PATH_32` | Path to the 32-bit profiler. Bitness-specific paths take precedence over generic paths. | `$INSTALL_DIR/win-x86/OpenTelemetry.AutoInstrumentation.Native.dll` for Windows                                                                                                                                                                                 |
| `COR_PROFILER_PATH_64`              | `CORECLR_PROFILER_PATH_64` | Path to the 64-bit profiler. Bitness-specific paths take precedence over generic paths. | `$INSTALL_DIR/win-x64/OpenTelemetry.AutoInstrumentation.Native.dll` for Windows                                                                                                                                                                                 |

Setting OpenTelemetry .NET Automatic Instrumentation as a .NET CLR Profiler
is required for .NET Framework.

On .NET, the .NET CLR Profiler is used only for [bytecode instrumentation](#instrumentations).
If having just [source instrumentation](#instrumentations) is acceptable,
you can unset or remove the following environment variables:

```env
COR_ENABLE_PROFILING
COR_PROFILER
COR_PROFILER_PATH_32
COR_PROFILER_PATH_64
CORECLR_ENABLE_PROFILING
CORECLR_PROFILER
CORECLR_PROFILER_PATH
CORECLR_PROFILER_PATH_32
CORECLR_PROFILER_PATH_64
OTEL_DOTNET_AUTO_INTEGRATIONS_FILE
```

## .NET Runtime

On .NET it is required to set the
[`DOTNET_STARTUP_HOOKS`](https://github.com/dotnet/runtime/blob/main/docs/design/features/host-startup-hook.md)
environment variable.

The [`DOTNET_ADDITIONAL_DEPS`](https://github.com/dotnet/runtime/blob/main/docs/design/features/additional-deps.md)
and [`DOTNET_SHARED_STORE`](https://docs.microsoft.com/en-us/dotnet/core/deploying/runtime-store)
environment variable are used to mitigate assembly version conflicts in .NET.

| Environment variable     | Required value                                                       |
|--------------------------|----------------------------------------------------------------------|
| `DOTNET_STARTUP_HOOKS`   | `$INSTALL_DIR/net/OpenTelemetry.AutoInstrumentation.StartupHook.dll` |
| `DOTNET_ADDITIONAL_DEPS` | `$INSTALL_DIR/AdditionalDeps`                                        |
| `DOTNET_SHARED_STORE`    | `$INSTALL_DIR/store`                                                 |

## Internal logs

The default directory paths for internal logs are:

- Windows: `%ProgramData%\OpenTelemetry .NET AutoInstrumentation\logs`
- Linux: `/var/log/opentelemetry/dotnet`
- macOS: `/var/log/opentelemetry/dotnet`

If the default log directories can't be created,
the instrumentation uses the path of the current user's [temporary folder](https://docs.microsoft.com/en-us/dotnet/api/System.IO.Path.GetTempPath?view=net-6.0)
instead.

| Environment variable                                | Description                                                             | Default value                            |
|-----------------------------------------------------|-------------------------------------------------------------------------|------------------------------------------|
| `OTEL_DOTNET_AUTO_LOG_DIRECTORY`                    | Directory of the .NET Tracer logs.                                      | _See the previous note on default paths_ |
| `OTEL_LOG_LEVEL`                                    | SDK log level. (supported values: `none`,`error`,`warn`,`info`,`debug`) | `info`                                   |
| `OTEL_DOTNET_AUTO_TRACES_CONSOLE_EXPORTER_ENABLED`  | Whether the traces console exporter is enabled or not.                  | `false`                                  |
| `OTEL_DOTNET_AUTO_METRICS_CONSOLE_EXPORTER_ENABLED` | Whether the metrics console exporter is enabled or not.                 | `false`                                  |
| `OTEL_DOTNET_AUTO_LOGS_CONSOLE_EXPORTER_ENABLED`    | Whether the logs console exporter is enabled or not.                    | `false`                                  |
| `OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE`   | Whether the log state should be formatted.                              | `false`                                  |
