# Configuration

## Global management

| Environment variable | Description | Default |
|-|-|-|
| `OTEL_DOTNET_AUTO_HOME` | Installation location. | `true` |
| `OTEL_DOTNET_AUTO_ENABLED` | Enable to activate the tracer. | `true` |
| `OTEL_DOTNET_AUTO_INCLUDE_PROCESSES` | Sets the filename of executables the profiler can attach to. If not defined (default), the profiler will attach to any process. Supports multiple values separated with comma, for example: `MyApp.exe,dotnet.exe` |  |
| `OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES` | Sets the filename of executables the profiler cannot attach to. If not defined (default), the profiler will attach to any process. Supports multiple values separated with comma, for example: `MyApp.exe,dotnet.exe` |  |
| `OTEL_DOTNET_AUTO_AZURE_APP_SERVICES` | Set to indicate that the profiler is running in the context of Azure App Services. | `false` |

## Resources

Resource is the immutable representation of the entity producing the telemetry.
See [Resource semantic conventions](https://github.com/open-telemetry/opentelemetry-specification/tree/main/specification/resource/semantic_conventions)
for more details.

| Environment variable | Description | Default |
|-|-|-|
| `OTEL_RESOURCE_ATTRIBUTES` | Key-value pairs to be used as resource attributes. See [Resource SDK](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/sdk.md#specifying-resource-information-via-an-environment-variable) for more details. | See [Resource semantic conventions](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/semantic_conventions/README.md#semantic-attributes-with-sdk-provided-default-value) for details. |
| `OTEL_SERVICE_NAME` | Sets the value of the [`service.name`](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/semantic_conventions/README.md#service) resource attribute. If `service.name` is also provided in `OTEL_RESOURCE_ATTRIBUTES`, then `OTEL_SERVICE_NAME` takes precedence. | `unknown_service:%ProcessName%` |

## Instrumentations

| Environment variable | Description | Default |
|-|-|-|
| `OTEL_DOTNET_AUTO_INTEGRATIONS_FILE` | The file path of bytecode instrumentations JSON configuration file. Usually it should be set to `%ProfilerDirectory%/integrations.json` | |
| `OTEL_DOTNET_AUTO_ENABLED_INSTRUMENTATIONS` | The source instrumentations you want to enable, separated by a comma. |  |
| `OTEL_DOTNET_AUTO_DISABLED_INSTRUMENTATIONS` | The source instrumentations set via `OTEL_DOTNET_AUTO_ENABLED_INSTRUMENTATIONS` value and bytecode instrumentations `OTEL_DOTNET_AUTO_INTEGRATIONS_FILE` set via configuration file you want to disable, separated by a comma. | |
| `OTEL_DOTNET_AUTO_{0}_ENABLED` | Configuration pattern for enabling or disabling a specific bytecode. For example, in order to disable MongoDb instrumentation, set `OTEL_TRACE_MongoDb_ENABLED=false` | `true` |
| `OTEL_DOTNET_AUTO_DOMAIN_NEUTRAL_INSTRUMENTATION` |  Sets whether to intercept method calls when the caller method is inside a domain-neutral assembly. This is recommended when instrumenting IIS applications. | `false` |
| `OTEL_DOTNET_AUTO_CLR_DISABLE_OPTIMIZATIONS` |  Set to `true` to disable all JIT optimizations. | `false` |
| `OTEL_DOTNET_AUTO_CLR_ENABLE_INLINING` | Set to `false` to disable JIT inlining. | `true` |
| `OTEL_DOTNET_AUTO_CLR_ENABLE_NGEN` | Set to `false` to disable NGEN images. | `true` |

### Instumented libraries and framework

| ID | Library | Instrumentation type |
|-|-|-|
| `AspNet` | ASP.NET and ASP.NET Core | source |
| `GraphQL` | [GraphQL](https://www.nuget.org/packages/GraphQL/) | bytecode |
| `HttpClient` | [System.Net.Http.HttpClient](https://docs.microsoft.com/dotnet/api/system.net.http.httpclient) and [System.Net.HttpWebRequest](https://docs.microsoft.com/dotnet/api/system.net.httpwebrequest) | source |
| `MongoDb` | [MongoDB.Driver.Core](https://www.nuget.org/packages/MongoDB.Driver.Core/) | bytecode |
| `SqlClient` | [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient) and [System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient) | source |

## ASP.NET (.NET Framework) Instrumentation

ASP.NET instrumentation on .NET Framework requires installing the
[`OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule`](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule/)
NuGet package on the instrumented project.
See [the WebConfig section](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.AspNet#step-2-modify-webconfig) for more information.

## Logging

Default logs directory paths are:

- Windows: `%ProgramData%\OpenTelemetry .NET AutoInstrumentation\logs`
- Linux: `/var/log/opentelemetry/dotnet`

| Environment variable | Description | Default |
|-|-|-|
| `OTEL_DOTNET_AUTO_LOG_DIRECTORY` | The directory of the .NET Tracer logs. Overrides the value in `OTEL_DOTNET_AUTO_LOG_PATH` if present. | _see above_ |
| `OTEL_DOTNET_AUTO_LOG_PATH` | The path of the profiler log file. | _see above_ |
| `OTEL_DOTNET_AUTO_DEBUG` | Enable to activate debugging mode for the tracer. | `false` |
| `OTEL_DOTNET_AUTO_CONSOLE_EXPORTER_ENABLED` | Defines whether the console exporter is enabled or not. | `false` |
| `OTEL_DOTNET_AUTO_DUMP_ILREWRITE_ENABLED` | Allows the profiler to dump the IL original code and modification to the log. | `false` |

## Exporters

The exporter is used to output the telemetry.

| Environment variable | Description | Default |
|-|-|-|
| `OTEL_TRACES_EXPORTER` | The traces exporter to be used. Available values are: `zipkin`, `jeager`, `otlp`, `none`. | `otlp` |
| `OTEL_EXPORTER_JAEGER_AGENT_HOST` | Hostname for the Jaeger agent. Used for `udp/thrift.compact` protocol.| `localhost` |
| `OTEL_EXPORTER_JAEGER_AGENT_PORT` | Port for the Jaeger agent. Used for `udp/thrift.compact` protocol. | `6831` |
| `OTEL_EXPORTER_JAEGER_ENDPOINT` | The Jaeger Collector HTTP endpoint. Used for `http/thrift.binary` protocol. | `http://localhost:14268` |
| `OTEL_EXPORTER_JAEGER_PROTOCOL` | The protocol to use for Jager exporter. Supported values: `udp/thrift.compact`, `http/thrift.binary` | `udp/thrift.compact` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Target endpoint for OTLP exporter. More details [here](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/exporter.md). | `http://localhost:4318` for `http/protobuf` protocol, `http://localhost:4317` for `grpc` protocol |
| `OTEL_EXPORTER_OTLP_HEADERS` | Key-value pairs to be used as headers associated with gRPC or HTTP requests. More details [here](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/exporter.md). | |
| `OTEL_EXPORTER_OTLP_TIMEOUT` | Maximum time the OTLP exporter will wait for each batch export. | `1000` (ms) |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | The OTLP expoter transport protocol. Supported values: `grpc`, `http/protobuf`. [1] | `http/protobuf` |
| `OTEL_EXPORTER_ZIPKIN_ENDPOINT` | Zipkin URL. | `http://localhost:8126` |
| `OTEL_DOTNET_AUTO_HTTP2UNENCRYPTEDSUPPORT_ENABLED` | Enables `[System.Net](http://system.net/).Http.SocketsHttpHandler.Http2UnencryptedSupport`. Required when instrumenting .NET Core 3.x applications and using an non-TLS gRPC OTLP endpoint. See [the official Microsoft documentation](https://docs.microsoft.com/en-us/aspnet/core/grpc/troubleshoot?view=aspnetcore-6.0#call-insecure-grpc-services-with-net-core-client) for more details. | `false` |

**[1]**: `OTEL_EXPORTER_OTLP_PROTOCOL` remarks:

- On .NET 5 and later, using the `grpc` OTLP exporter protocol requires the application
  to reference [`Grpc.Net.Client`](https://www.nuget.org/packages/Grpc.Net.Client/).
  E.g. by adding `<PackageReference Include="Grpc.Net.Client" Version="2.32.0" />` to the `.csproj` file.
- On .NET Framework, using the `grpc` OTLP exporter protocol is not supported.
  
## Batch Span Processor

The Batch Span Processor batches finished spans before they are sent by the exporter.

| Environment variable | Description | Default |
|-|-|-|
| `OTEL_BSP_SCHEDULE_DELAY` | Delay interval between two consecutive exports. | `5000` (ms) |
| `OTEL_BSP_EXPORT_TIMEOUT` | Maximum allowed time to export data. | `30000` (ms) |
| `OTEL_BSP_MAX_QUEUE_SIZE` | Maximum queue size. | `2048` |
| `OTEL_BSP_MAX_EXPORT_BATCH_SIZE` | Maximum batch size. Must be less than or equal to `OTEL_BSP_MAX_QUEUE_SIZE`. | `512` (ms) |

## Customization

| Environment variable | Description | Default |
|-|-|-|
| `OTEL_DOTNET_AUTO_LOAD_AT_STARTUP` | Defines whether the tracer is created by the auto instrumentation library or not. | `true` |
| `OTEL_DOTNET_AUTO_ADDITIONAL_SOURCES` | Comma separated list of additional `ActivitySource` names to be added to the tracer at the startup. |  |
| `OTEL_DOTNET_AUTO_LEGACY_SOURCES` | Comma separated list of additional legacy source names to be added to the tracer at the startup. |  |
| `OTEL_DOTNET_AUTO_INSTRUMENTATION_PLUGINS` | Colon (:) separated list of OTel SDK instrumentation plugins represented by `System.Type.AssemblyQualifiedName`. | |

`OTEL_DOTNET_AUTO_LOAD_AT_STARTUP` should be set to `false` when application
initializes OpenTelemetry .NET SDK Tracer on its own. This configuration can be
used e.g. to just get the bytecode instrumentations.

`OTEL_DOTNET_AUTO_ADDITIONAL_SOURCES` should be used to capture manually
instrumented spans.

`OTEL_DOTNET_AUTO_LEGACY_SOURCES` can be used to capture `Activity` objects
created without using the `ActivitySource` API.

`OTEL_DOTNET_AUTO_INSTRUMENTATION_PLUGINS` can be used to extend the
configuration of the the OpenTelemetry .NET SDK Tracer. A plugin must be a
non-static, non-abstract class which has a default constructor and a method
with following signature:

```csharp
public OpenTelemetry.Trace.TracerProviderBuilder ConfigureTracerProvider(OpenTelemetry.Trace.TracerProviderBuilder builder)
```

The plugin must use the same version of the `OpenTelemetry` as the
OpenTelemetry .NET AutoInstrumentation. Current version is `1.2.0-rc3`.

## .NET CLR Profiler

To perform bytecode instrumentation, configure the OpenTelemetry .NET
auto-instrumentation as a .NET CLR Profiler.
Below are the environment variables used by CLR to setup the profiler.

| Environment variable | Description | Value |
|-|-|-|
| `CORECLR_ENABLE_PROFILING` | Enable the profiler. | `1` |
| `CORECLR_PROFILER` | CLSID of the profiler. | `30000` (ms) |
| `CORECLR_PROFILER_PATH` | Path to the profiler. | `%InstallationLocation%/OpenTelemetry.AutoInstrumentation.Native.so` for Linux, `%InstallationLocation%/OpenTelemetry.AutoInstrumentation.Native.dylib` for MacOS |
| `CORECLR_PROFILER_PATH_32` | Path to the 32 bit profiler. | `%InstallationLocation%/win-x86/OpenTelemetry.AutoInstrumentation.Native.dll` for Windows |
| `CORECLR_PROFILER_PATH_64` | Path to the 64 bit profiler. | `%InstallationLocation%/win-x64/OpenTelemetry.AutoInstrumentation.Native.dll` for Windows |

Remarks:

1. .NET Framework uses `CLR_` prefix instead of `CORECLR_`.
2. The bitness-specific variables take precedence.
3. The `*_PROFILER_PATH_*` environment variable is not needed on Windows
   if the `.dll` is registered.

Reference: [.NET Runtime Profiler Loading](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/profiling/Profiler%20Loading.md).

## .NET Runtime Additional-Deps and Package Store

To resolve assembly version conflicts in .NET Core,
set the
[`DOTNET_ADDITIONAL_DEPS`](https://github.com/dotnet/runtime/blob/main/docs/design/features/additional-deps.md)
and [`DOTNET_SHARED_STORE`](https://docs.microsoft.com/en-us/dotnet/core/deploying/runtime-store)
environment variables to the following values:

| Environment variable | Value |
|-|-|
| `DOTNET_ADDITIONAL_DEPS` | `%InstallationLocation%/AdditionalDeps` |
| `DOTNET_SHARED_STORE` | `%InstallationLocation%/store` |
