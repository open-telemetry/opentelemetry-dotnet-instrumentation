# Configuration

## Global settings

| Environment variable                 | Description                                                                                                                                                                                                                  | Default value |
|--------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------|
| `OTEL_DOTNET_AUTO_HOME`              | Installation location.                                                                                                                                                                                                       |               |
| `OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES` | Names of the executable files that the profiler cannot instrument. Supports multiple comma-separated values, for example: `ReservedProcess.exe,powershell.exe`. If unset, the profiler attaches to all processes by default. |               |

## Resources

A resource is the immutable representation of the entity producing the telemetry.
See [Resource semantic conventions](https://github.com/open-telemetry/opentelemetry-specification/tree/main/specification/resource/semantic_conventions)
for more details.

| Environment variable       | Description                                                                                                                                                                                                                                                                                                       | Default value                                                                                                                                                                                                                       |
|----------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `OTEL_RESOURCE_ATTRIBUTES` | Key-value pairs to be used as resource attributes. See [Resource SDK](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/sdk.md#specifying-resource-information-via-an-environment-variable) for more details.                                                        | See [Resource semantic conventions](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/semantic_conventions/README.md#semantic-attributes-with-sdk-provided-default-value) for details. |
| `OTEL_SERVICE_NAME`        | Sets the value of the [`service.name`](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/semantic_conventions/README.md#service) resource attribute. If `service.name` is provided in `OTEL_RESOURCE_ATTRIBUTES`, the value of `OTEL_SERVICE_NAME` takes precedence. | `unknown_service:%ProcessName%`                                                                                                                                                                                                     |

## Instrumentations

| Environment variable                                 | Description                                                                                                                                                                                                                                                     | Default value                  |
|------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------|
| `OTEL_DOTNET_AUTO_INTEGRATIONS_FILE`                 | List of bytecode instrumentations JSON configuration filepaths, delimited by the platform-specific path separator (`;` on Windows, `:` on Linux and macOS). For example: `%ProfilerDirectory%/integrations.json`. It is required for bytecode instrumentations. |                                |
| `OTEL_DOTNET_AUTO_TRACES_ENABLED_INSTRUMENTATIONS`   | Comma-separated list of traces source instrumentations you want to enable. Set to `none` to disable all trace instrumentations.                                                                                                                                 | all available instrumentations |
| `OTEL_DOTNET_AUTO_TRACES_DISABLED_INSTRUMENTATIONS`  | Comma-separated list of traces source and bytecode instrumentations you want to disable.                                                                                                                                                                        |                                |
| `OTEL_DOTNET_AUTO_METRICS_ENABLED_INSTRUMENTATIONS`  | Comma-separated list of metrics source instrumentations you want to enable. Set to `none` to disable all metric instrumentations.                                                                                                                               | all available instrumentations |
| `OTEL_DOTNET_AUTO_METRICS_DISABLED_INSTRUMENTATIONS` | Comma-separated list of metrics source instrumentations you want to disable.                                                                                                                                                                                    |                                |
| `OTEL_DOTNET_AUTO_LOGS_ENABLED_INSTRUMENTATIONS`     | Comma-separated list of logs source instrumentations you want to enable. Set to `none` to disable all metric instrumentations.                                                                                                                                  | all available instrumentations |
| `OTEL_DOTNET_AUTO_LOGS_DISABLED_INSTRUMENTATIONS`    | Comma-separated list of logs source instrumentations you want to disable.                                                                                                                                                                                       |                                |

### Traces instrumentations

| ID                   | Instrumented library                                                                                                                                                                            | Supported versions | Instrumentation type    |
|----------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------|-------------------------|
| `AspNet`             | ASP.NET Framework                                                                                                                                                                               | *                  | source                  |
| `AspNet`             | ASP.NET Core                                                                                                                                                                                    | *                  | source                  |
| `GraphQL`            | [GraphQL](https://www.nuget.org/packages/GraphQL/)                                                                                                                                              | ≥2.3.0 & < 3.0.0   | bytecode                |
| `GrpcNetClient`      | [Grpc.Net.Client](https://www.nuget.org/packages/Grpc.Net.Client)                                                                                                                               | ≥2.43.0 & < 3.0.0  | source                  |
| `HttpClient`         | [System.Net.Http.HttpClient](https://docs.microsoft.com/dotnet/api/system.net.http.httpclient) and [System.Net.HttpWebRequest](https://docs.microsoft.com/dotnet/api/system.net.httpwebrequest) | *                  | source                  |
| `MassTransit`        | [MassTransit](https://www.nuget.org/packages/MassTransit) **Not supported on .NET Framework**                                                                                                   | ≥8.0.0             | source                  |
| `MongoDB`            | [MongoDB.Driver.Core](https://www.nuget.org/packages/MongoDB.Driver.Core) **Not supported on .NET Framework**                                                                                   | ≥2.13.3 & < 3.0.0  | source & bytecode       |
| `MySqlData`          | [MySql.Data](https://www.nuget.org/packages/MySql.Data) **Not supported on .NET Framework**                                                                                                     | ≥6.10.7            | source & bytecode \[1\] |
| `Npgsql`             | [Npgsql](https://www.nuget.org/packages/Npgsql)                                                                                                                                                 | ≥6.0.0             | source                  |
| `SqlClient`          | [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient) and [System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient)                           | *                  | source                  |
| `StackExchangeRedis` | [StackExchange.Redis](https://www.nuget.org/packages/StackExchange.Redis) **Not supported on .NET Framework**                                                                                   | ≥2.0.405 < 3.0.0   | source & bytecode       |
| `Wcf`                | [System.ServiceModel](https://www.nuget.org/packages/System.ServiceModel) **No support for server side on .NET**. For configuration see [WCF Instrumentation Configuration](wcf-config.md)      | * \[2\]            | source                  |

\[1\]: MySql.Data 8.0.31 and higher requires bytecode instrumentation.

\[2\]: On .NET it supports [System.ServiceModel.Primitives](https://www.nuget.org/packages/System.ServiceModel.Primitives)
≥ 4.7.0.

### Metrics instrumentations

| ID           | Instrumented library                                                                                                                                                                            | Supported versions | Instrumentation type |
|--------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------|----------------------|
| `AspNet`     | ASP.NET Framework \[1\]                                                                                                                                                                         | *                  | source               |
| `AspNet`     | ASP.NET Core \[2\]                                                                                                                                                                              | *                  | source               |
| `HttpClient` | [System.Net.Http.HttpClient](https://docs.microsoft.com/dotnet/api/system.net.http.httpclient) and [System.Net.HttpWebRequest](https://docs.microsoft.com/dotnet/api/system.net.httpwebrequest) | *                  | source               |
| `NetRuntime` | [OpenTelemetry.Instrumentation.Runtime](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Runtime)                                                                                   | *                  | source               |
| `Process`    | [OpenTelemetry.Instrumentation.Process](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Process)                                                                                   | *                  | source               |

\[1\]: The ASP.NET metrics are generated only if the `AspNet` trace instrumentation
 is also enabled.

\[2\]: This instrumentation automatically enables the
 `Microsoft.AspNetCore.Hosting.HttpRequestIn` spans.

### Logs instrumentations

| ID      | Instrumented library                                                                                                            | Supported versions | Instrumentation type   |
|---------|---------------------------------------------------------------------------------------------------------------------------------|--------------------|------------------------|
| ILogger | [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging) **Not supported on .NET Framework** | ≥6.0.0             | bytecode or source [1] |

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

| Environment variable    | Description                                                                                            | Default value |
|-------------------------|--------------------------------------------------------------------------------------------------------|---------------|
| `OTEL_TRACES_EXPORTER`  | Traces exporter to be used. The value can be one of the following: `zipkin`, `jaeger`, `otlp`, `none`. | `otlp`        |
| `OTEL_METRICS_EXPORTER` | Metrics exporter to be used. The value can be one of the following: `otlp`, `prometheus`, `none`.      | `otlp`        |
| `OTEL_LOGS_EXPORTER`    | Logs exporter to be used. The value can be one of the following: `otlp`, `none`.                       | `otlp`        |

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
[OTLP exporter documentation](https://github.com/open-telemetry/opentelemetry-dotnet/tree/core-1.4.0-beta.3/src/OpenTelemetry.Exporter.OpenTelemetryProtocol#environment-variables).
Important environment variables include:

| Environment variable                     | Description                                                                                                                                                                                                | Default value                                                                                             |
|------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------|
| `OTEL_EXPORTER_OTLP_ENDPOINT`            | Target endpoint for the OTLP exporter. See [the OpenTelemetry specification](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/exporter.md) for more details. | `http://localhost:4318` for the `http/protobuf` protocol, `http://localhost:4317` for the `grpc` protocol |
| `OTEL_EXPORTER_OTLP_PROTOCOL`            | OTLP exporter transport protocol. Supported values are `grpc`, `http/protobuf`. [1]                                                                                                                        | `http/protobuf`                                                                                           |
| `OTEL_EXPORTER_OTLP_TIMEOUT`             | The max waiting time (in milliseconds) for the backend to process each batch.                                                                                                                              | `10000`                                                                                                   |
| `OTEL_EXPORTER_OTLP_HEADERS`             | Comma-separated list of additional HTTP headers sent with each export, for example: `Authorization=secret,X-Key=Value`.                                                                                    |                                                                                                           |
| `OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT`      | Maximum allowed attribute value size.                                                                                                                                                                      | none                                                                                                      |
| `OTEL_ATTRIBUTE_COUNT_LIMIT`             | Maximum allowed span attribute count.                                                                                                                                                                      | none                                                                                                      |
| `OTEL_SPAN_ATTRIBUTE_VALUE_LENGTH_LIMIT` | Maximum allowed attribute value size. [Not applicable for metrics.](https://github.com/open-telemetry/opentelemetry-specification/blob/v1.15.0/specification/metrics/sdk.md#attribute-limits).             | none                                                                                                      |
| `OTEL_SPAN_ATTRIBUTE_COUNT_LIMIT`        | Maximum allowed span attribute count. [Not applicable for metrics.](https://github.com/open-telemetry/opentelemetry-specification/blob/v1.15.0/specification/metrics/sdk.md#attribute-limits).             | none                                                                                                      |
| `OTEL_SPAN_EVENT_COUNT_LIMIT`            | Maximum allowed span event count.                                                                                                                                                                          | none                                                                                                      |
| `OTEL_SPAN_LINK_COUNT_LIMIT`             | Maximum allowed span link count.                                                                                                                                                                           | none                                                                                                      |
| `OTEL_EVENT_ATTRIBUTE_COUNT_LIMIT`       | Maximum allowed attribute per span event count.                                                                                                                                                            | none                                                                                                      |
| `OTEL_LINK_ATTRIBUTE_COUNT_LIMIT`        | Maximum allowed attribute per span link count.                                                                                                                                                             | none                                                                                                      |

**[1]**: Considerations on the `OTEL_EXPORTER_OTLP_PROTOCOL`:

- The OpenTelemetry .NET Automatic Instrumentation defaults to `http/protobuf`,
  which differs from the OpenTelemetry .NET SDK default value of `grpc`.
- On .NET 6 and higher, the application must reference [`Grpc.Net.Client`](https://www.nuget.org/packages/Grpc.Net.Client/)
  to use the `grpc` OTLP exporter protocol. For example, by adding
  `<PackageReference Include="Grpc.Net.Client" Version="2.43.0" />` to the
  `.csproj` file.
- On .NET Framework, the `grpc` OTLP exporter protocol is not supported.

### Jaeger

To enable the Jaeger exporter, set the `OTEL_TRACES_EXPORTER` environment variable
to `jaeger`.

To customize the Jaeger exporter using environment variables, see the
[Jaeger exporter documentation](https://github.com/open-telemetry/opentelemetry-dotnet/tree/core-1.4.0-beta.3/src/OpenTelemetry.Exporter.Jaeger#environment-variables).
Important environment variables include:

| Environment variable              | Description                                                                                          | Default value                       |
|-----------------------------------|------------------------------------------------------------------------------------------------------|-------------------------------------|
| `OTEL_EXPORTER_JAEGER_AGENT_HOST` | Host name for the Jaeger agent. Used for the `udp/thrift.compact` protocol.                          | `localhost`                         |
| `OTEL_EXPORTER_JAEGER_AGENT_PORT` | Port for the Jaeger agent. Used for the `udp/thrift.compact` protocol.                               | `6831`                              |
| `OTEL_EXPORTER_JAEGER_ENDPOINT`   | Jaeger Collector HTTP endpoint. Used for the `http/thrift.binary` protocol.                          | `http://localhost:14268/api/traces` |
| `OTEL_EXPORTER_JAEGER_PROTOCOL`   | Protocol to use for Jaeger exporter. Supported values are `udp/thrift.compact`, `http/thrift.binary` | `udp/thrift.compact`                |

### Prometheus

> ⚠️ **Do NOT use in production.**
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
[Prometheus Exporter HttpListener documentation](https://github.com/open-telemetry/opentelemetry-dotnet/tree/core-1.4.0-beta.3/src/OpenTelemetry.Exporter.Prometheus.HttpListener).
to learn more.

### Zipkin

To enable the Zipkin exporter, set the `OTEL_TRACES_EXPORTER` environment
variable to `zipkin`.

To customize the Zipkin exporter using environment variables,
see the [Zipkin exporter documentation](https://github.com/open-telemetry/opentelemetry-dotnet/tree/core-1.4.0-beta.3/src/OpenTelemetry.Exporter.Zipkin#configuration-using-environment-variables).
Important environment variables include:

| Environment variable            | Description | Default value                        |
|---------------------------------|-------------|--------------------------------------|
| `OTEL_EXPORTER_ZIPKIN_ENDPOINT` | Zipkin URL  | `http://localhost:9411/api/v2/spans` |

## Additional settings

| Environment variable                           | Description                                                                                                                                                                                                                                                                                                                                                                                        | Default value |
|------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------|
| `OTEL_DOTNET_AUTO_TRACES_ENABLED`              | Enables traces.                                                                                                                                                                                                                                                                                                                                                                                    | `true`        |
| `OTEL_DOTNET_AUTO_OPENTRACING_ENABLED`         | Enables OpenTracing tracer.                                                                                                                                                                                                                                                                                                                                                                        | `false`       |
| `OTEL_DOTNET_AUTO_LOGS_ENABLED`                | Enables logs.                                                                                                                                                                                                                                                                                                                                                                                      | `true`        |
| `OTEL_DOTNET_AUTO_METRICS_ENABLED`             | Enables metrics.                                                                                                                                                                                                                                                                                                                                                                                   | `true`        |
| `OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES`   | Comma-separated list of additional `System.Diagnostics.ActivitySource` names to be added to the tracer at the startup. Use it to capture manually instrumented spans.                                                                                                                                                                                                                              |               |
| `OTEL_DOTNET_AUTO_LEGACY_SOURCES`              | Comma-separated list of additional legacy source names to be added to the tracer at the startup. Use it to capture `System.Diagnostics.Activity` objects created without using the `System.Diagnostics.ActivitySource` API.                                                                                                                                                                        |               |
| `OTEL_DOTNET_AUTO_FLUSH_ON_UNHANDLEDEXCEPTION` | Controls whether the telemetry data is flushed when an [AppDomain.UnhandledException](https://docs.microsoft.com/en-us/dotnet/api/system.appdomain.unhandledexception) event is raised. Set to `true` when you suspect that you are experiencing a problem with missing telemetry data and also experiencing unhandled exceptions.                                                                 | `false`       |
| `OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES`  | Comma-separated list of additional `System.Diagnostics.Metrics.Meter` names to be added to the meter at the startup. Use it to capture manually instrumented spans.                                                                                                                                                                                                                                |               |
| `OTEL_DOTNET_AUTO_PLUGINS`                     | Colon-separated list of OTel SDK instrumentation plugin types, specified with the [assembly-qualified name](https://docs.microsoft.com/en-us/dotnet/api/system.type.assemblyqualifiedname?view=net-6.0#system-type-assemblyqualifiedname). _Note: This list must be colon-separated because the type names may include commas._ See more info on how to write plugins at [plugins.md](plugins.md). |               |

## .NET CLR Profiler

The CLR uses the following
environment variables to set up the profiler. See
[.NET Runtime Profiler Loading](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/profiling/Profiler%20Loading.md)
for more information.

| .NET Framework environment variable | .NET environment variable  | Description                                                                             | Required value                                                                                                                                                                                                         |
|-------------------------------------|----------------------------|-----------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `COR_ENABLE_PROFILING`              | `CORECLR_ENABLE_PROFILING` | Enables the profiler.                                                                   | `1`                                                                                                                                                                                                                    |
| `COR_PROFILER`                      | `CORECLR_PROFILER`         | CLSID of the profiler.                                                                  | `{918728DD-259F-4A6A-AC2B-B85E1B658318}`                                                                                                                                                                               |
| `COR_PROFILER_PATH`                 | `CORECLR_PROFILER_PATH`    | Path to the profiler.                                                                   | `$INSTALL_DIR/OpenTelemetry.AutoInstrumentation.Native.dll` for Windows, `$INSTALL_DIR/OpenTelemetry.AutoInstrumentation.Native.so` for Linux, `$INSTALL_DIR/OpenTelemetry.AutoInstrumentation.Native.dylib` for macOS |
| `COR_PROFILER_PATH_32`              | `CORECLR_PROFILER_PATH_32` | Path to the 32-bit profiler. Bitness-specific paths take precedence over generic paths. | `$INSTALL_DIR/win-x86/OpenTelemetry.AutoInstrumentation.Native.dll` for Windows                                                                                                                                        |
| `COR_PROFILER_PATH_64`              | `CORECLR_PROFILER_PATH_64` | Path to the 64-bit profiler. Bitness-specific paths take precedence over generic paths. | `$INSTALL_DIR/win-x64/OpenTelemetry.AutoInstrumentation.Native.dll` for Windows                                                                                                                                        |

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

| Environment variable                                | Description                                             | Default value                            |
|-----------------------------------------------------|---------------------------------------------------------|------------------------------------------|
| `OTEL_DOTNET_AUTO_LOG_DIRECTORY`                    | Directory of the .NET Tracer logs.                      | _See the previous note on default paths_ |
| `OTEL_DOTNET_AUTO_DEBUG`                            | Enables debugging mode for the tracer.                  | `false`                                  |
| `OTEL_DOTNET_AUTO_TRACES_CONSOLE_EXPORTER_ENABLED`  | Whether the traces console exporter is enabled or not.  | `false`                                  |
| `OTEL_DOTNET_AUTO_METRICS_CONSOLE_EXPORTER_ENABLED` | Whether the metrics console exporter is enabled or not. | `false`                                  |
| `OTEL_DOTNET_AUTO_LOGS_CONSOLE_EXPORTER_ENABLED`    | Whether the logs console exporter is enabled or not.    | `false`                                  |
| `OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE`   | Whether the log state should be formatted.              | `false`                                  |
