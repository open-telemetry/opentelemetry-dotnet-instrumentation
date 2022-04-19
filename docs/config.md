# Configuration

## Global settings

| Environment variable | Description | Default |
|-|-|-|
| `OTEL_DOTNET_AUTO_HOME` | Installation location. | `true` |
| `OTEL_DOTNET_AUTO_ENABLED` | Enables the tracer. | `true` |
| `OTEL_DOTNET_AUTO_INCLUDE_PROCESSES` | Names of the executable files that the profiler can instrument. Supports multiple comma-separated values, for example: `MyApp.exe,dotnet.exe`. If unset, the profiler attaches to all processes by default. |  |
| `OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES` | Names of the executable files that the profiler cannot instrument. Supports multiple comma-separated values, for example: `ReservedProcess.exe,powershell.exe`. The list is processed after `OTEL_DOTNET_AUTO_INCLUDE_PROCESSES`. If unset, the profiler attaches to all processes by default. |  |
| `OTEL_DOTNET_AUTO_AZURE_APP_SERVICES` | Set to indicate that the profiler is running in the context of Azure App Services. | `false` |
 

## Resources

A resource is the immutable representation of the entity producing the telemetry.
See [Resource semantic conventions](https://github.com/open-telemetry/opentelemetry-specification/tree/main/specification/resource/semantic_conventions)
for more details.

| Environment variable | Description | Default |
|-|-|-|
| `OTEL_RESOURCE_ATTRIBUTES` | Key-value pairs to be used as resource attributes. See [Resource SDK](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/sdk.md#specifying-resource-information-via-an-environment-variable) for more details. | See [Resource semantic conventions](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/semantic_conventions/README.md#semantic-attributes-with-sdk-provided-default-value) for details. |
| `OTEL_SERVICE_NAME` | Sets the value of the [`service.name`](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/semantic_conventions/README.md#service) resource attribute. If `service.name` is provided in `OTEL_RESOURCE_ATTRIBUTES`, the value of `OTEL_SERVICE_NAME` takes precedence. | `unknown_service:%ProcessName%` |

## Instrumentations

| Environment variable | Description | Default |
|-|-|-|
| `OTEL_DOTNET_AUTO_INTEGRATIONS_FILE` | File path of JSON configuration files of bytecode instrumentations. For example: `%ProfilerDirectory%/integrations.json` | |
| `OTEL_DOTNET_AUTO_ENABLED_INSTRUMENTATIONS` | Comma-separated list of source instrumentations you want to enable. |  |
| `OTEL_DOTNET_AUTO_DISABLED_INSTRUMENTATIONS` | Comma-separated list of source and bytecode instrumentations you want to disable. | |
| `OTEL_DOTNET_AUTO_{0}_ENABLED` | Configuration pattern for enabling or disabling specific bytecode. For example, to disable GraphQL instrumentation, set the `OTEL_TRACE_GraphQL_ENABLED` environment variable to `false`. | `true` |
| `OTEL_DOTNET_AUTO_DOMAIN_NEUTRAL_INSTRUMENTATION` | Whether to intercept method calls when the caller method is inside a domain-neutral assembly. Useful when instrumenting IIS applications. | `false` |
| `OTEL_DOTNET_AUTO_CLR_DISABLE_OPTIMIZATIONS` |  Set to `true` to disable all JIT optimizations. | `false` |
| `OTEL_DOTNET_AUTO_CLR_ENABLE_INLINING` | Set to `false` to disable JIT inlining. | `true` |
| `OTEL_DOTNET_AUTO_CLR_ENABLE_NGEN` | Set to `false` to disable NGEN images. | `true` |

### Instrumented libraries and frameworks

| ID | Library | Instrumentation type |
|-|-|-|
| `AspNet` | ASP.NET and ASP.NET Core | source |
| `GraphQL` | [GraphQL](https://www.nuget.org/packages/GraphQL/) | bytecode |
| `HttpClient` | [System.Net.Http.HttpClient](https://docs.microsoft.com/dotnet/api/system.net.http.httpclient) and [System.Net.HttpWebRequest](https://docs.microsoft.com/dotnet/api/system.net.httpwebrequest) | source |
| `SqlClient` | [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient) and [System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient) | source |

## ASP.NET (.NET Framework) Instrumentation

ASP.NET instrumentation on .NET Framework requires to install the
[`OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule`](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule/)
NuGet package in the instrumented project.
See [the WebConfig section](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.AspNet#step-2-modify-webconfig) for more information.

## Logging

The default log directory paths are:

- Windows: `%ProgramData%\OpenTelemetry .NET AutoInstrumentation\logs`
- Linux: `/var/log/opentelemetry/dotnet`

If the default log directories can't be created,
the instrumentation uses the path of the current user's [temporary folder](https://docs.microsoft.com/en-us/dotnet/api/System.IO.Path.GetTempPath?view=net-6.0) instead.

| Environment variable | Description | Default |
|-|-|-|
| `OTEL_DOTNET_AUTO_LOG_DIRECTORY` | Directory of the .NET Tracer logs. | _See the previous note on default paths_ |
| `OTEL_DOTNET_AUTO_DEBUG` | Enables debugging mode for the tracer. | `false` |
| `OTEL_DOTNET_AUTO_CONSOLE_EXPORTER_ENABLED` | Whether the console exporter is enabled or not. | `false` |
| `OTEL_DOTNET_AUTO_DUMP_ILREWRITE_ENABLED` | Lets the profiler dump the IL original code and modification to the log. | `false` |

## Exporters

Exporters output the telemetry.

| Environment variable | Description | Default |
|-|-|-|
| `OTEL_TRACES_EXPORTER` | Traces exporter to be used. The value can be one of the following: `zipkin`, `jaeger`, `otlp`, `none`. | `otlp` |
| `OTEL_EXPORTER_JAEGER_AGENT_HOST` | Host name for the Jaeger agent. Used for the `udp/thrift.compact` protocol.| `localhost` |
| `OTEL_EXPORTER_JAEGER_AGENT_PORT` | Port for the Jaeger agent. Used for the `udp/thrift.compact` protocol. | `6831` |
| `OTEL_EXPORTER_JAEGER_ENDPOINT` | Jaeger Collector HTTP endpoint. Used for the `http/thrift.binary` protocol. | `http://localhost:14268` |
| `OTEL_EXPORTER_JAEGER_PROTOCOL` | Protocol to use for Jager exporter. Supported values are `udp/thrift.compact`, `http/thrift.binary` | `udp/thrift.compact` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Target endpoint for the OTLP exporter. See [the OpenTelemetry specification](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/exporter.md) for more details. | `http://localhost:4318` for the `http/protobuf` protocol, `http://localhost:4317` for the `grpc` protocol |
| `OTEL_EXPORTER_OTLP_HEADERS` | Key-value pairs to be used as headers associated with gRPC or HTTP requests. See the [OpenTelemetry specification](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/exporter.md) for more details. | |
| `OTEL_EXPORTER_OTLP_TIMEOUT` | Maximum amount of time the OTLP exporter waits for each batch export. | `1000` (ms) |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | OTLP exporter transport protocol. Supported values are `grpc`, `http/protobuf`. [1] | `http/protobuf` |
| `OTEL_EXPORTER_ZIPKIN_ENDPOINT` | Zipkin URL. | `http://localhost:8126` |
| `OTEL_DOTNET_AUTO_HTTP2UNENCRYPTEDSUPPORT_ENABLED` | Enables `System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport`. Required when instrumenting .NET Core 3.x applications while using a non-TLS endpoint for gRPC OTLP. See the [official Microsoft documentation](https://docs.microsoft.com/en-us/aspnet/core/grpc/troubleshoot?view=aspnetcore-6.0#call-insecure-grpc-services-with-net-core-client) for more details. | `false` |

**[1]**: Considerations on the `OTEL_EXPORTER_OTLP_PROTOCOL`:

- On .NET 5 and higher, the application must reference [`Grpc.Net.Client`](https://www.nuget.org/packages/Grpc.Net.Client/)
  to use the `grpc` OTLP exporter protocol. For example, by adding 
  `<PackageReference Include="Grpc.Net.Client" Version="2.32.0" />` to the `.csproj` file.
- On .NET Framework, the `grpc` OTLP exporter protocol is not supported.
  
## Batch span processor

The batch span processor batches finished spans before sending them through the exporter.

| Environment variable | Description | Default |
|-|-|-|
| `OTEL_BSP_SCHEDULE_DELAY` | Delay interval between two consecutive exports. | `5000` (ms) |
| `OTEL_BSP_EXPORT_TIMEOUT` | Maximum allowed time to export data. | `30000` (ms) |
| `OTEL_BSP_MAX_QUEUE_SIZE` | Maximum queue size. | `2048` |
| `OTEL_BSP_MAX_EXPORT_BATCH_SIZE` | Maximum batch size. Must be less than or equal to `OTEL_BSP_MAX_QUEUE_SIZE`. | `512` (ms) |

## Additional settings

| Environment variable | Description | Default |
|-|-|-|
| `OTEL_DOTNET_AUTO_LOAD_AT_STARTUP` | Whether the tracer is created by the automatic instrumentation library or not. Set to `false` when the application initializes the OpenTelemetry .NET SDK Tracer on its own. This configuration can be used, for example, to retrieve the bytecode instrumentations. | `true` |
| `OTEL_DOTNET_AUTO_ADDITIONAL_SOURCES` | Comma-separated list of additional `ActivitySource` names to be added to the tracer at the startup. Use it to capture manually instrumented spans. |  |
| `OTEL_DOTNET_AUTO_LEGACY_SOURCES` | Comma-separated list of additional legacy source names to be added to the tracer at the startup. Use it to capture `Activity` objects created without using the `ActivitySource` API. |  |
| `OTEL_DOTNET_AUTO_FLUSH_ON_UNHANDLEDEXCEPTION` | Controls whether the telemetry data is flushed when an [AppDomain.UnhandledException](https://docs.microsoft.com/en-us/dotnet/api/system.appdomain.unhandledexception) event is raised. Set to `true` when you suspect that you are experiencing a problem with missing telemetry data and also experiencing unhandled exceptions. | `false` |
| `OTEL_DOTNET_AUTO_INSTRUMENTATION_PLUGINS` | Colon-separated list of OTel SDK instrumentation plugins represented by `System.Type.AssemblyQualifiedName`. | |


You can use `OTEL_DOTNET_AUTO_INSTRUMENTATION_PLUGINS` to extend the
configuration of the OpenTelemetry .NET SDK Tracer. A plugin must be a
non-static, non-abstract class which has a default constructor and a method
with following signature:

```csharp
public OpenTelemetry.Trace.TracerProviderBuilder ConfigureTracerProvider(OpenTelemetry.Trace.TracerProviderBuilder builder)
```

The plugin must use the same version of the `OpenTelemetry` as the
OpenTelemetry .NET Automatic Instrumentation.

## .NET CLR Profiler

To perform bytecode instrumentation, configure the OpenTelemetry .NET
Automatic Instrumentation as a .NET CLR Profiler. The CLR uses the following
environment variables to set up the profiler. 

> Notice that .NET Framework uses the `COR_` prefix instead of `CORECLR_`.

| Environment variable | Description | Value |
|-|-|-|
| `CORECLR_ENABLE_PROFILING` | Enables the profiler. | `1` |
| `CORECLR_PROFILER` | CLSID of the profiler. | `30000` (ms) |
| `CORECLR_PROFILER_PATH` | Path to the profiler. | `%InstallationLocation%/OpenTelemetry.AutoInstrumentation.Native.so` for Linux, `%InstallationLocation%/OpenTelemetry.AutoInstrumentation.Native.dylib` for MacOS |
| `CORECLR_PROFILER_PATH_32` | Path to the 32-bit profiler. Bitness-specific paths take precedence over generic paths. | `%InstallationLocation%/win-x86/OpenTelemetry.AutoInstrumentation.Native.dll` for Windows |
| `CORECLR_PROFILER_PATH_64` | Path to the 64-bit profiler. Bitness-specific paths take precedence over generic paths. | `%InstallationLocation%/win-x64/OpenTelemetry.AutoInstrumentation.Native.dll` for Windows |

The `*_PROFILER_PATH_*` environment variable is not needed on Windows if the DLL file is already registered.

See [.NET Runtime Profiler Loading](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/profiling/Profiler%20Loading.md) for more information.

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
