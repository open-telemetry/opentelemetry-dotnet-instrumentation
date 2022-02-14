# Usage

**WARNING**: Please notice that no official release has been created for this repo yet, so installation instructions below would require you to build the code manually beforehand.

## Configuration

### Global management settings

| Environment variable | Description | Default |
|-|-|-|
| `OTEL_DOTNET_AUTO_HOME` | Installation location. | `true` |
| `OTEL_DOTNET_AUTO_ENABLED` | Enable to activate the tracer. | `true` |
| `OTEL_DOTNET_AUTO_INCLUDE_PROCESSES` | Sets the filename of executables the profiler can attach to. If not defined (default), the profiler will attach to any process. Supports multiple values separated with comma, for example: `MyApp.exe,dotnet.exe` |  |
| `OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES` | Sets the filename of executables the profiler cannot attach to. If not defined (default), the profiler will attach to any process. Supports multiple values separated with comma, for example: `MyApp.exe,dotnet.exe` |  |
| `OTEL_DOTNET_AUTO_AZURE_APP_SERVICES` | Set to indicate that the profiler is running in the context of Azure App Services. | `false` |

### Resource

Resource is the immutable representation of the entity producing the telemetry.
See [Resource semantic conventions](https://github.com/open-telemetry/opentelemetry-specification/tree/main/specification/resource/semantic_conventions)
for more details.

| Environment variable | Description | Default |
|-|-|-|
| `OTEL_RESOURCE_ATTRIBUTES` | Key-value pairs to be used as resource attributes. See [Resource SDK](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/sdk.md#specifying-resource-information-via-an-environment-variable) for more details. | See [Resource semantic conventions](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/semantic_conventions/README.md#semantic-attributes-with-sdk-provided-default-value) for details. |
| `OTEL_SERVICE_NAME` | Sets the value of the [`service.name`](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/semantic_conventions/README.md#service) resource attribute. If `service.name` is also provided in `OTEL_RESOURCE_ATTRIBUTES`, then `OTEL_SERVICE_NAME` takes precedence. | `unknown_service:%ProcessName%` |

### Instrumentations

| Environment variable | Description | Default |
|-|-|-|
| `OTEL_DOTNET_AUTO_INTEGRATIONS_FILE` | The file path of bytecode instrumentations JSON configuration file. Usually it should be set to `%ProfilerDirectory%/integrations.json` | |
| `OTEL_DOTNET_AUTO_ENABLED_INSTRUMENTATIONS` | The instrumentations you want to enable, separated by a comma. Supported values: `AspNet`, `HttpClient`, `SqlClient`, `MongoDb`. |  |
| `OTEL_DOTNET_AUTO_DISABLED_INSTRUMENTATIONS` | The instrumentations set via `OTEL_DOTNET_AUTO_ENABLED_INSTRUMENTATIONS` value and `OTEL_DOTNET_AUTO_INTEGRATIONS_FILE` configuration file you want to disable, separated by a comma. | |
| `OTEL_DOTNET_AUTO_{0}_ENABLED` | Configuration pattern for enabling or disabling a specific bytecode. For example, in order to disable MongoDb instrumentation, set `OTEL_TRACE_MongoDb_ENABLED=false` | `true` |
| `OTEL_DOTNET_AUTO_DOMAIN_NEUTRAL_INSTRUMENTATION` |  Sets whether to intercept method calls when the caller method is inside a domain-neutral assembly. This is recommended when instrumenting IIS applications. | `false` |
| `OTEL_DOTNET_AUTO_CLR_DISABLE_OPTIMIZATIONS` |  Set to `true` to disable all JIT optimizations. | `false` |
| `OTEL_CLR_ENABLE_INLINING` | Set to `false` to disable JIT inlining. | `true` |
| `OTEL_CLR_ENABLE_NGEN` | Set to `false` to disable NGEN images. | `true` |

### ASP.NET (.NET Framework) Instrumentation

ASP.NET instrumentation on .NET Framework requires installing the
[`OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule`](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule/)
NuGet package on the instrumented project.
More info [here](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.AspNet#step-2-modify-webconfig).

### Logging

Default logs directory paths are:

- Windows: `%ProgramData%\OpenTelemetry .NET AutoInstrumentation\logs`
- Linux: `/var/log/opentelemetry/dotnet`

| Environment variable | Description | Default |
|-|-|-|
| `OTEL_TRACE_LOG_DIRECTORY` | The directory of the .NET Tracer logs. Overrides the value in `OTEL_TRACE_LOG_PATH` if present. | _see above_ |
| `OTEL_TRACE_LOG_PATH` | The path of the profiler log file. | _see above_ |
| `OTEL_TRACE_DEBUG` | Enable to activate debugging mode for the tracer. | `false` |
| `OTEL_DOTNET_TRACER_CONSOLE_EXPORTER_ENABLED` | Defines whether the console exporter is enabled or not. | `true` |
| `OTEL_DUMP_ILREWRITE_ENABLED` | Allows the profiler to dump the IL original code and modification to the log. | `false` |

### Exporters

The exporter is used to output the telemetry.

| Environment variable | Description | Default |
|-|-|-|
| `OTEL_TRACES_EXPORTER` | The traces exporter to be used. Available values are: `zipkin`, `jeager`, `otlp`. | `otlp` |
| `OTEL_EXPORTER_JAEGER_AGENT_HOST` | Hostname for the Jaeger agent. | `localhost` |
| `OTEL_EXPORTER_JAEGER_AGENT_PORT` | Port for the Jaeger agent. | `6831` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Target endpoint for OTLP exporter. More details [here](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/exporter.md). | `http://localhost:4318` for `http/protobuf` protocol, `http://localhost:4317` for `grpc` protocol |
| `OTEL_EXPORTER_OTLP_HEADERS` | Key-value pairs to be used as headers associated with gRPC or HTTP requests. More details [here](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/exporter.md). | |
| `OTEL_EXPORTER_OTLP_TIMEOUT` | Maximum time the OTLP exporter will wait for each batch export. | `1000` (ms) |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | The OTLP expoter transport protocol. Supported values: `grpc`, `http/protobuf`. [1] | `http/protobuf` |
| `OTEL_EXPORTER_ZIPKIN_ENDPOINT` | Zipkin URL. | `http://localhost:8126` |

**[1]**: `OTEL_EXPORTER_OTLP_PROTOCOL` remarks:

- On .NET 5 and later, using the `grpc` OTLP exporter protocol requires the application
  to reference [`Grpc.Net.Client`](https://www.nuget.org/packages/Grpc.Net.Client/).
  E.g. by adding `<PackageReference Include="Grpc.Net.Client" Version="2.32.0" />` to the `.csproj` file.
- On .NET Framework, using the `grpc` OTLP exporter protocol is not supported.
  
### Batch Span Processor

The Batch Span Processor batches of finished spans before they are send by the exporter.

| Environment variable | Description | Default |
|-|-|-|
| `OTEL_BSP_SCHEDULE_DELAY` | Delay interval between two consecutive exports. | `5000` (ms) |
| `OTEL_BSP_EXPORT_TIMEOUT` | Maximum allowed time to export data. | `30000` (ms) |
| `OTEL_BSP_MAX_QUEUE_SIZE` | Maximum queue size. | `2048` |
| `OTEL_BSP_MAX_EXPORT_BATCH_SIZE` | Maximum batch size. Must be less than or equal to `OTEL_BSP_MAX_QUEUE_SIZE`. | `512` (ms) |

### Customization

| Environment variable | Description | Default |
|-|-|-|
| `OTEL_DOTNET_TRACER_LOAD_AT_STARTUP` | Defines whether the tracer is created by the auto instrumentation library or not. | `true` |
| `OTEL_DOTNET_TRACER_ADDITIONAL_SOURCES` | Comma separated list of additional `ActivitySource` names to be added to the tracer at the startup. |  |
| `OTEL_DOTNET_TRACER_LEGACY_SOURCES` | Comma separated list of additional legacy source names to be added to the tracer at the startup. |  |
| `OTEL_DOTNET_TRACER_INSTRUMENTATION_PLUGINS` | Colon (:) separated list of OTel SDK instrumentation plugins represented by `System.Type.AssemblyQualifiedName`. | |

`OTEL_DOTNET_TRACER_LOAD_AT_STARTUP` should be set to `false` when application
initializes OpenTelemetry .NET SDK Tracer on its own. This configuration can be
used e.g. to just get the bytecode instrumentations.

`OTEL_DOTNET_TRACER_ADDITIONAL_SOURCES` should be used to capture manually
instrumented spans.

`OTEL_DOTNET_TRACER_LEGACY_SOURCES` can be used to capture `Activity` objects
created without using the `ActivitySource` API.

`OTEL_DOTNET_TRACER_INSTRUMENTATION_PLUGINS` can be used to extend the
configuration of the the OpenTelemetry .NET SDK Tracer. A plugin must be a
non-static, non-abstract class which has a default constructor and a method
with following signature:

```csharp
public OpenTelemetry.Trace.TracerProviderBuilder ConfigureTracerProvider(OpenTelemetry.Trace.TracerProviderBuilder builder)
```

The plugin must use the same version of the `OpenTelemetry` as the
OpenTelemetry .NET AutoInstrumentation. Current version is `1.2.0-rc2`.

## Troubleshooting

Check if you are not hitting one of the issues listed below.

### Handling of Assembly version Conflicts

OpenTelemetry SDK NuGet package are deployed with the OpenTelemetry .NET Instrumentation.
Conflicts in assemblies referenced by "source instrumentations", should be handled in
the same way: updating the project references to ensure that they are the same version
as the one used by the instrumentation.

However, the workarounds proposed above only work at build time. When a rebuild is not
possible, there are ways to force the application to use the assembly versions shipped
together with the instrumentation.

For .NET Framework applications, the workaround is to use Binding Redirects. For .NET Core
applications, the workaround is to manipulate the `deps.json`. Currently, we do not
automate any of these workarounds, but we are manually validating them.

#### .NET Framework Binding Redirects

The [samples/BindingRedirect](./samples/BindingRedirect/) app shows how
to use the `app.config` to solve the version conflicts. As configured in the PoC branch,
the BindingRedirect sample can only run successfully under the instrumentation since the
binding redirect makes the application dependent on a version of `System.Diagnostics.DiagnosticSource`
that is not available during building time.

#### .NET Core Dependency File

To fix assembly version conflicts in .NET Core, the `<application>.deps.json`, generated by default
during the build of .NET Core applications, needs to be modified. Build a .NET Core app with package
references using the required version and use the respective `deps.json` file to see what changes
are needed.

To experiment with modifying the `deps.json` file, add a reference to the required version OpenTelemetry
package to the [samples/CoreAppOldReference](./samples/CoreAppOldReference/) sample and rebuild the
application. Save the generated `deps.json` file, remove the package reference and rebuild the
sample app. Compare the files to understand the changes.

### Linux instrumentation not working

The proper binary needs to be selected when deploying to Linux, eg.: the default Microsoft .NET images are
based on Debian and should use the `deb` package, see the [Linux](#Linux) setup section.

If you are not sure what is the Linux distribution being used try the following commands:

```terminal
lsb_release -a
cat /etc/*release
cat /etc/issue*
cat /proc/version
```

### High CPU usage

The default installation of auto-instrumentation enables tracing all .NET processes on the box.
In the typical scenarios (dedicated VMs or containers), this is not a problem.
Use the environment variables `OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES` and `OTEL_DOTNET_AUTO_INCLUDE_PROCESSES`
to include/exclude applications from the tracing auto-instrumentation.
These are ";" delimited lists that control the inclusion/exclusion of processes.

### No proper relatioship between spans

On .NET Framework strong name signing can force multiple versions of the same assembly being loaded on the same process.
This causes a separate hierarchy of Activity objects. If you are referencing packages in your application that use
different version of the `System.Diagnostics.DiagnosticSource` than the `OpenTelemetry.Api` used by autoinstrumentation
(`6.0.0`) you have to explicitly reference the `System.Diagnostics.DiagnosticSource` package in the correct version
in your application (see [custom instrumentation section](#configure-custom-instrumentation)).
This will cause automatic binding redirection to occur resolving the issue.

If automatic binding redirection is [disabled](https://docs.microsoft.com/en-us/dotnet/framework/configure-apps/how-to-enable-and-disable-automatic-binding-redirection)
you can also manually add binding redirection to [the `App.config` file](../samples/BindingRedirect/App.config).

### Investigating other issues

If none of the suggestions above solves your issue, detailed logs are necessary.
Follow the steps below to get the detailed logs from OpenTelemetry AutoInstrumentation for .NET:

Set the environment variable `OTEL_TRACE_DEBUG` to `true` before the instrumented process starts.
By default, the library writes the log files under the below predefined locations.
If needed, change the default location by updating the environment variable `OTEL_TRACE_LOG_PATH` to an appropriate path.
On Linux, the default log location is `/var/log/opentelemetry/dotnet/`
On Windows, the default log location is `%ProgramData%\\OpenTelemetry .NET AutoInstrumentation\logs\`
Compress the whole folder to capture the multiple log files and send the compressed folder to us.
After obtaining the logs, remember to remove the environment variable `OTEL_TRACE_DEBUG` to avoid unnecessary overhead.
