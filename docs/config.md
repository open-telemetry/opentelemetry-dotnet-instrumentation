# Configuration

## Global settings

| Environment variable | Description | Default value |
|-|-|-|
| `OTEL_DOTNET_AUTO_HOME` | Installation location. |  |
| `OTEL_DOTNET_AUTO_TRACES_ENABLED` | Enables the tracer. | `true` |
| `OTEL_DOTNET_AUTO_METRICS_ENABLED` | Enables the meter. | `true` |
| `OTEL_DOTNET_AUTO_INCLUDE_PROCESSES` | Names of the executable files that the profiler can instrument. Supports multiple comma-separated values, for example: `MyApp.exe,dotnet.exe`. If unset, the profiler attaches to all processes by default. |  |
| `OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES` | Names of the executable files that the profiler cannot instrument. Supports multiple comma-separated values, for example: `ReservedProcess.exe,powershell.exe`. The list is processed after `OTEL_DOTNET_AUTO_INCLUDE_PROCESSES`. If unset, the profiler attaches to all processes by default. |  |

## Resources

A resource is the immutable representation of the entity producing the telemetry.
See [Resource semantic conventions](https://github.com/open-telemetry/opentelemetry-specification/tree/main/specification/resource/semantic_conventions)
for more details.

| Environment variable | Description | Default value |
|-|-|-|
| `OTEL_RESOURCE_ATTRIBUTES` | Key-value pairs to be used as resource attributes. See [Resource SDK](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/sdk.md#specifying-resource-information-via-an-environment-variable) for more details. | See [Resource semantic conventions](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/semantic_conventions/README.md#semantic-attributes-with-sdk-provided-default-value) for details. |
| `OTEL_SERVICE_NAME` | Sets the value of the [`service.name`](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/semantic_conventions/README.md#service) resource attribute. If `service.name` is provided in `OTEL_RESOURCE_ATTRIBUTES`, the value of `OTEL_SERVICE_NAME` takes precedence. | `unknown_service:%ProcessName%` |

## Instrumentations

| Environment variable | Description | Default value |
|-|-|-|
| `OTEL_DOTNET_AUTO_INTEGRATIONS_FILE` | File path of JSON configuration files of bytecode instrumentations. For example: `%ProfilerDirectory%/integrations.json` | |
| `OTEL_DOTNET_AUTO_TRACES_ENABLED_INSTRUMENTATIONS` | Comma-separated list of traces source instrumentations you want to enable. |  |
| `OTEL_DOTNET_AUTO_TRACES_DISABLED_INSTRUMENTATIONS` | Comma-separated list of traces source and bytecode instrumentations you want to disable. | |
| `OTEL_DOTNET_AUTO_METRICS_ENABLED_INSTRUMENTATIONS` | Comma-separated list of metrics source instrumentations you want to enable. |  |
| `OTEL_DOTNET_AUTO_METRICS_DISABLED_INSTRUMENTATIONS` | Comma-separated list of metrics source instrumentations you want to disable. | |
| `OTEL_DOTNET_AUTO_{0}_ENABLED` | Configuration pattern for enabling or disabling specific bytecode. For example, to disable GraphQL instrumentation, set the `OTEL_TRACE_GraphQL_ENABLED` environment variable to `false`. | `true` |
| `OTEL_DOTNET_AUTO_DOMAIN_NEUTRAL_INSTRUMENTATION` | Whether to intercept method calls when the caller method is inside a domain-neutral assembly. Useful when instrumenting IIS applications. | `false` |

### Instrumented traces libraries and frameworks

| ID | Library | Supported versions | Instrumentation type |
|-|-|-|-|
| `AspNet` | ASP.NET MVC Framework | * | source |
| `AspNet` | ASP.NET Core | * | source |
| `GraphQL` | [GraphQL](https://www.nuget.org/packages/GraphQL/) | ≥2.3.0 & < 3.0.0 | bytecode |
| `HttpClient` | [System.Net.Http.HttpClient](https://docs.microsoft.com/dotnet/api/system.net.http.httpclient) and [System.Net.HttpWebRequest](https://docs.microsoft.com/dotnet/api/system.net.httpwebrequest) | * | source |
| `MongoDB` | [MongoDB.Driver.Core](https://www.nuget.org/packages/MongoDB.Driver.Core) **Not supported on .NET Framework**  | ≥2.3.0 & < 3.0.0 | source & bytecode |
| `SqlClient` | [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient) and [System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient) | * | source |

### Instrumented metrics libraries and frameworks

| ID | Library | Supported versions | Instrumentation type |
|-|-|-|-|
| `AspNet` | ASP.NET MVC Framework | * | source |
| `AspNet` | ASP.NET Core | * | source |
| `HttpClient` | [System.Net.Http.HttpClient](https://docs.microsoft.com/dotnet/api/system.net.http.httpclient) and [System.Net.HttpWebRequest](https://docs.microsoft.com/dotnet/api/system.net.httpwebrequest) | * | source |
| `NetRuntime` | [OpenTelemetry.Instrumentation.Runtime](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Runtime) | * | source |

## Logging

The default log directory paths are:

- Windows: `%ProgramData%\OpenTelemetry .NET AutoInstrumentation\logs`
- Linux: `/var/log/opentelemetry/dotnet`

If the default log directories can't be created,
the instrumentation uses the path of the current user's [temporary folder](https://docs.microsoft.com/en-us/dotnet/api/System.IO.Path.GetTempPath?view=net-6.0)
instead.

| Environment variable | Description | Default value |
|-|-|-|
| `OTEL_DOTNET_AUTO_LOG_DIRECTORY` | Directory of the .NET Tracer logs. | _See the previous note on default paths_ |
| `OTEL_DOTNET_AUTO_DEBUG` | Enables debugging mode for the tracer. | `false` |
| `OTEL_DOTNET_AUTO_TRACES_CONSOLE_EXPORTER_ENABLED` | Whether the traces console exporter is enabled or not. | `false` |
| `OTEL_DOTNET_AUTO_METRICS_CONSOLE_EXPORTER_ENABLED` | Whether the metrics console exporter is enabled or not. | `false` |

## Exporters

Exporters output the telemetry.

| Environment variable | Description | Default value |
|-|-|-|
| `OTEL_TRACES_EXPORTER` | Traces exporter to be used. The value can be one of the following: `zipkin`, `jaeger`, `otlp`, `none`. | `otlp` |
| `OTEL_METRICS_EXPORTER` | Metrics exporter to be used. The value can be one of the following: `otlp`, `prometheus`, `none`. | `otlp` |

### Jaeger

To enable the Jaeger exporter, set the `OTEL_TRACES_EXPORTER` environment variable
to `jaeger`. To customize the Jaeger exporter using environment variables, see the
[Jaeger exporter documentation](https://github.com/open-telemetry/opentelemetry-dotnet/tree/core-1.3.0/src/OpenTelemetry.Exporter.Jaeger#environment-variables).
Important environment variables include:

| Environment variable | Description | Default value |
|-|-|-|
| `OTEL_EXPORTER_JAEGER_AGENT_HOST` | Host name for the Jaeger agent. Used for the `udp/thrift.compact` protocol.| `localhost` |
| `OTEL_EXPORTER_JAEGER_AGENT_PORT` | Port for the Jaeger agent. Used for the `udp/thrift.compact` protocol. | `6831` |
| `OTEL_EXPORTER_JAEGER_ENDPOINT` | Jaeger Collector HTTP endpoint. Used for the `http/thrift.binary` protocol. | `http://localhost:14268` |
| `OTEL_EXPORTER_JAEGER_PROTOCOL` | Protocol to use for Jaeger exporter. Supported values are `udp/thrift.compact`, `http/thrift.binary` | `udp/thrift.compact` |

### OTLP

To enable the OTLP exporter, set the `OTEL_TRACES_EXPORTER` environment variable
to `otlp`. To customize the OTLP exporter using environment variables, see the
[OTLP exporter documentation](https://github.com/open-telemetry/opentelemetry-dotnet/tree/core-1.3.0/src/OpenTelemetry.Exporter.OpenTelemetryProtocol#environment-variables).
Important environment variables include:

| Environment variable | Description | Default value |
|-|-|-|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Target endpoint for the OTLP exporter. See [the OpenTelemetry specification](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/exporter.md) for more details. | `http://localhost:4318` for the `http/protobuf` protocol, `http://localhost:4317` for the `grpc` protocol |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | OTLP exporter transport protocol. Supported values are `grpc`, `http/protobuf`. [1] | `http/protobuf` |
| `OTEL_EXPORTER_OTLP_TIMEOUT` | The max waiting time (in milliseconds) for the backend to process each batch. | `10000` |
| `OTEL_EXPORTER_OTLP_HEADERS` | Comma-separated list of additional HTTP headers sent with each export, for example: `Authorization=secret,X-Key=Value`. | |

**[1]**: Considerations on the `OTEL_EXPORTER_OTLP_PROTOCOL`:

- The OpenTelemetry .NET Automatic Instrumentation defaults to `http/protobuf`,
  which differs from the OpenTelemetry .NET SDK default value of `grpc`.
- On .NET 6 and higher, the application must reference [`Grpc.Net.Client`](https://www.nuget.org/packages/Grpc.Net.Client/)
  to use the `grpc` OTLP exporter protocol. For example, by adding
  `<PackageReference Include="Grpc.Net.Client" Version="2.32.0" />` to the
  `.csproj` file.
- On .NET Framework, the `grpc` OTLP exporter protocol is not supported.

The OpenTelemetry .NET Automatic Instrumentation also supports the following
environment variables for the OTLP exporter:
| Environment variable | Description | Default value |
|-|-|-|
| `OTEL_DOTNET_AUTO_HTTP2UNENCRYPTEDSUPPORT_ENABLED` | Enables `System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport`. Required when instrumenting .NET Core 3.x applications while using a non-TLS endpoint for gRPC OTLP. See the [official Microsoft documentation](https://docs.microsoft.com/en-us/aspnet/core/grpc/troubleshoot?view=aspnetcore-6.0#call-insecure-grpc-services-with-net-core-client) for more details. | `false` |

### Zipkin

To enable the Zipkin exporter, set the `OTEL_TRACES_EXPORTER` environment
variable to `zipkin`. To customize the Zipkin exporter using environment
variables, see the [Zipkin exporter documentation](https://github.com/open-telemetry/opentelemetry-dotnet/tree/core-1.3.0/src/OpenTelemetry.Exporter.Zipkin#configuration-using-environment-variables).
Important environment variables include:

| Environment variable | Description | Default value |
|-|-|-|
| `OTEL_EXPORTER_ZIPKIN_ENDPOINT` | Zipkin URL. | `http://localhost:8126` |

## Additional settings

| Environment variable | Description | Default value |
|-|-|-|
| `OTEL_DOTNET_AUTO_LOAD_TRACER_AT_STARTUP` | Whether the tracer is created by the automatic instrumentation library or not. Set to `false` when the application initializes the OpenTelemetry .NET SDK Tracer on its own. This configuration can be used, for example, to retrieve the bytecode instrumentations. | `true` |
| `OTEL_DOTNET_AUTO_LOAD_METER_AT_STARTUP` | Whether the meter is created by the automatic instrumentation library or not. Set to `false` when the application initializes the OpenTelemetry .NET SDK Meter on its own. | `true` |
| `OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES` | Comma-separated list of additional `System.Diagnostics.ActivitySource` names to be added to the tracer at the startup. Use it to capture manually instrumented spans. |  |
| `OTEL_DOTNET_AUTO_LEGACY_SOURCES` | Comma-separated list of additional legacy source names to be added to the tracer at the startup. Use it to capture `System.Diagnostics.Activity` objects created without using the `System.Diagnostics.ActivitySource` API. |  |
| `OTEL_DOTNET_AUTO_FLUSH_ON_UNHANDLEDEXCEPTION` | Controls whether the telemetry data is flushed when an [AppDomain.UnhandledException](https://docs.microsoft.com/en-us/dotnet/api/system.appdomain.unhandledexception) event is raised. Set to `true` when you suspect that you are experiencing a problem with missing telemetry data and also experiencing unhandled exceptions. | `false` |
| `OTEL_DOTNET_AUTO_TRACES_PLUGINS` | Colon-separated list of OTel SDK instrumentation tracer plugin types, specified with the [assembly-qualified name](https://docs.microsoft.com/en-us/dotnet/api/system.type.assemblyqualifiedname?view=net-6.0#system-type-assemblyqualifiedname). _Note: This list must be colon-separated because the type names may include commas._ | |
| `OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES` | Comma-separated list of additional `System.Diagnostics.Metrics.Meter` names to be added to the meter at the startup. Use it to capture manually instrumented spans. |  |
| `OTEL_DOTNET_AUTO_METRICS_PLUGINS` | Colon-separated list of OTel SDK instrumentation meter plugin types, specified with the [assembly-qualified name](https://docs.microsoft.com/en-us/dotnet/api/system.type.assemblyqualifiedname?view=net-6.0#system-type-assemblyqualifiedname). _Note: This list must be colon-separated because the type names may include commas._ | |

You can use `OTEL_DOTNET_AUTO_TRACES_PLUGINS` to extend the
configuration of the OpenTelemetry .NET SDK Tracer. A plugin must be a
non-static, non-abstract class which has a default constructor and a method
with following signature:

```csharp
public OpenTelemetry.Trace.TracerProviderBuilder ConfigureTracerProvider(OpenTelemetry.Trace.TracerProviderBuilder builder)
```

You can use `OTEL_DOTNET_AUTO_METRICS_PLUGINS` to extend the
configuration of the OpenTelemetry .NET SDK Meter. A plugin must be a
non-static, non-abstract class which has a default constructor and a method
with following signature:

```csharp
public OpenTelemetry.Metrics.MeterProviderBuilder ConfigureMeterProvider(OpenTelemetry.Metrics.MeterProviderBuilder builder)
```

The plugin must use the same version of the `OpenTelemetry` as the
OpenTelemetry .NET Automatic Instrumentation.

## .NET CLR Profiler

To perform bytecode instrumentation, configure the OpenTelemetry .NET
Automatic Instrumentation as a .NET CLR Profiler. The CLR uses the following
environment variables to set up the profiler. See
[.NET Runtime Profiler Loading](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/profiling/Profiler%20Loading.md)
for more information.

| .NET Framework environment variable | .NET Core environment variable | Description | Required value |
|-|-|-|-|
| `COR_ENABLE_PROFILING` | `CORECLR_ENABLE_PROFILING` | Enables the profiler. | `1` |
| `COR_PROFILER` | `CORECLR_PROFILER` | CLSID of the profiler. | `{918728DD-259F-4A6A-AC2B-B85E1B658318}` |
| `COR_PROFILER_PATH` | `CORECLR_PROFILER_PATH` | Path to the profiler. | `%InstallationLocation%/OpenTelemetry.AutoInstrumentation.Native.dll` for Windows, `%InstallationLocation%/OpenTelemetry.AutoInstrumentation.Native.so` for Linux, `%InstallationLocation%/OpenTelemetry.AutoInstrumentation.Native.dylib` for MacOS |
| `COR_PROFILER_PATH_32` | `CORECLR_PROFILER_PATH_32` | Path to the 32-bit profiler. Bitness-specific paths take precedence over generic paths. | `%InstallationLocation%/win-x86/OpenTelemetry.AutoInstrumentation.Native.dll` for Windows |
| `COR_PROFILER_PATH_64` | `CORECLR_PROFILER_PATH_64` | Path to the 64-bit profiler. Bitness-specific paths take precedence over generic paths. | `%InstallationLocation%/win-x64/OpenTelemetry.AutoInstrumentation.Native.dll` for Windows |

## .NET Runtime additional dependencies and package store

To resolve assembly version conflicts in .NET Core,
set the
[`DOTNET_ADDITIONAL_DEPS`](https://github.com/dotnet/runtime/blob/main/docs/design/features/additional-deps.md)
and [`DOTNET_SHARED_STORE`](https://docs.microsoft.com/en-us/dotnet/core/deploying/runtime-store)
environment variables to the following values:

| Environment variable | Value |
|-|-|
| `DOTNET_ADDITIONAL_DEPS` | `%InstallationLocation%/AdditionalDeps` |
| `DOTNET_SHARED_STORE` | `%InstallationLocation%/store` |
