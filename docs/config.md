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
    - `OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED`
    - `OTEL_DOTNET_AUTO_[TRACES|METRICS|LOGS]_INSTRUMENTATION_ENABLED`
    - `OTEL_DOTNET_AUTO_[TRACES|METRICS|LOGS]_{INSTRUMENTATION_ID}_INSTRUMENTATION_ENABLED`
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

3. Service name automatic detection

   If no service name is explicitly configured one will be generated for you.
     This can be helpful in some circumstances.
     - If the application is hosted on IIS in .NET Framework this will be
     `SiteName\VirtualPath` ex: `MySite\MyApp`
     - If that is not the case it will use the name of the application [entry Assembly](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.assembly.getentryassembly?view=net-7.0).

By default we recommend using environment variables for configuration.
However, if given setting supports it, then:

- use `Web.config` for configuring an ASP.NET application (.NET Framework),
- use `App.config` for configuring a Windows Service (.NET Framework).

## Global settings

| Environment variable                 | Description                                                                                                                                                                                                                             | Default value | Status                                                                                                                            |
|--------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------|-----------------------------------------------------------------------------------------------------------------------------------|
| `OTEL_DOTNET_AUTO_HOME`              | Installation location.                                                                                                                                                                                                                  |               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES` | Names of the executable files that the profiler cannot instrument. Supports multiple comma-separated values, for example: `ReservedProcess.exe,powershell.exe`. If unset, the profiler attaches to all processes by default. \[1\]\[2\] |               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED` | Enables possibility to fail process when automatic instrumentation cannot be executed. It is designed for debugging purposes. It should not be used in production environment. \[1\]                                                    | `false`       | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_LOG_LEVEL`                     | SDK log level. (supported values: `none`,`error`,`warn`,`info`,`debug`)                                                                                                                                                                 | `info`        | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md)       |

\[1\] If `OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED` is set to `true` then processes
excluded from instrumentation by `OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES` will fail
instead of silently continue.
\[2\] Notice that applications launched via `dotnet MyApp.dll` have process
name `dotnet` or `dotnet.exe`.

## Resources

A resource is the immutable representation of the entity producing the telemetry.
See [Resource semantic conventions](https://github.com/open-telemetry/semantic-conventions/tree/main/docs/resource)
for more details.

### Resource attributes

| Environment variable       | Description                                                                                                                                                                                                                                                                  | Default value                                                                                                                                                                                  | Status                                                                                                                      |
|----------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------|
| `OTEL_RESOURCE_ATTRIBUTES` | Key-value pairs to be used as resource attributes. See [Resource SDK](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/sdk.md#specifying-resource-information-via-an-environment-variable) for more details.                   | See [Resource semantic conventions](https://github.com/open-telemetry/semantic-conventions/blob/main/docs/resource/README.md#semantic-attributes-with-sdk-provided-default-value) for details. | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_SERVICE_NAME`        | Sets the value of the [`service.name`](https://github.com/open-telemetry/semantic-conventions/blob/main/docs/resource/README.md#service) resource attribute. If `service.name` is provided in `OTEL_RESOURCE_ATTRIBUTES`, the value of `OTEL_SERVICE_NAME` takes precedence. | See [Service name automatic detection](#configuration-methods) under Configuration method section.                                                                                             | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |

### Resource detectors

| Environment variable                             | Description                                                                                                                                                                                           | Default value | Status                                                                                                                            |
|--------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------|-----------------------------------------------------------------------------------------------------------------------------------|
| `OTEL_DOTNET_AUTO_RESOURCE_DETECTOR_ENABLED`     | Enables all resource detectors.                                                                                                                                                                       | `true`        | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_{0}_RESOURCE_DETECTOR_ENABLED` | Configuration pattern for enabling a specific resource detector, where `{0}` is the uppercase id of the resource detector you want to enable. Overrides `OTEL_DOTNET_AUTO_RESOURCE_DETECTOR_ENABLED`. | `true`        | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |

The following resource detectors are included and enabled by default:

| ID                | Description                | Documentation                                                                                                                                                                                                                         | Status                                                                                                                            |
|-------------------|----------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------|
| `AZUREAPPSERVICE` | Azure App Service detector | [Azure resource detector documentation](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/Resources.Azure-1.0.0-beta.8/src/OpenTelemetry.Resources.Azure/README.md)                                                 | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `CONTAINER`       | Container detector         | [Container resource detector documentation](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/Resources.Container-1.0.0-beta.9/src/OpenTelemetry.Resources.Container/README.md) **Not supported on .NET Framework** | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `HOST`            | Host detector              | [Host resource detector documentation](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/Resources.Host-0.1.0-beta.2/src/OpenTelemetry.Resources.Host/README.md)                                                    | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OPERATINGSYSTEM` | Operating System detector  | [Operating System resource detector documentation](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/Resources.OperatingSystem-0.1.0-alpha.2/src/OpenTelemetry.Resources.OperatingSystem/README.md)                                                    | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `PROCESS`         | Process detector           | [Process resource detector documentation](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/Resources.Process-0.1.0-beta.2/src/OpenTelemetry.Resources.Process/README.md)                                           | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `PROCESSRUNTIME`  | Process Runtime detector   | [Process Runtime resource detector documentation](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/Resources.ProcessRuntime-0.1.0-beta.2/src/OpenTelemetry.Resources.ProcessRuntime/README.md)                     | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |

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

| Environment variable                                   | Description                                                                                                                                                                                                    | Default value                                                                          | Status                                                                                                                            |
|--------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------|
| `OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED`             | Enables all instrumentations.                                                                                                                                                                                  | `true`                                                                                 | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED`      | Enables all trace instrumentations. Overrides `OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED`.                                                                                                                      | Inherited from the current value of `OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED`         | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_TRACES_{0}_INSTRUMENTATION_ENABLED`  | Configuration pattern for enabling a specific trace instrumentation, where `{0}` is the uppercase id of the instrumentation you want to enable. Overrides `OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED`.   | Inherited from the current value of `OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED`  | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_METRICS_INSTRUMENTATION_ENABLED`     | Disables all metric instrumentations. Overrides `OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED`.                                                                                                                    | Inherited from the current value of `OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED`         | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_METRICS_{0}_INSTRUMENTATION_ENABLED` | Configuration pattern for enabling a specific metric instrumentation, where `{0}` is the uppercase id of the instrumentation you want to enable. Overrides `OTEL_DOTNET_AUTO_METRICS_INSTRUMENTATION_ENABLED`. | Inherited from the current value of `OTEL_DOTNET_AUTO_METRICS_INSTRUMENTATION_ENABLED` | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_LOGS_INSTRUMENTATION_ENABLED`        | Disables all log instrumentations. Overrides `OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED`.                                                                                                                       | Inherited from the current value of `OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED`         | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_LOGS_{0}_INSTRUMENTATION_ENABLED`    | Configuration pattern for enabling a specific log instrumentation, where `{0}` is the uppercase id of the instrumentation you want to enable. Overrides `OTEL_DOTNET_AUTO_LOGS_INSTRUMENTATION_ENABLED`.       | Inherited from the current value of `OTEL_DOTNET_AUTO_LOGS_INSTRUMENTATION_ENABLED`    | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |

### Traces instrumentations

**Status**: [Mixed](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md).
Traces are stable, but particular instrumentation are in Experimental status
due to lack of stable semantic convention.

| ID                    | Instrumented library                                                                                                                                                                                               | Supported versions     | Instrumentation type | Status                                                                                                                            |
|-----------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------|----------------------|-----------------------------------------------------------------------------------------------------------------------------------|
| `ASPNET`              | ASP.NET (.NET Framework) MVC / WebApi \[1\] **Not supported on .NET**                                                                                                                                              | * \[2\]                | source & bytecode    | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `ASPNETCORE`          | ASP.NET Core **Not supported on .NET Framework**                                                                                                                                                                   | *                      | source               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `AZURE`               | [Azure SDK](https://azure.github.io/azure-sdk/releases/latest/index.html)                                                                                                                                          | \[3\]                  | source               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `ELASTICSEARCH`       | [Elastic.Clients.Elasticsearch](https://www.nuget.org/packages/Elastic.Clients.Elasticsearch)                                                                                                                      | * \[4\]                | source               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `ELASTICTRANSPORT`    | [Elastic.Transport](https://www.nuget.org/packages/Elastic.Transport)                                                                                                                                              | ≥0.4.16                | source               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `ENTITYFRAMEWORKCORE` | [Microsoft.EntityFrameworkCore](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore) **Not supported on .NET Framework**                                                                                  | ≥6.0.12                | source               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `GRAPHQL`             | [GraphQL](https://www.nuget.org/packages/GraphQL) **Not supported on .NET Framework**                                                                                                                              | ≥7.5.0                 | source               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `GRPCNETCLIENT`       | [Grpc.Net.Client](https://www.nuget.org/packages/Grpc.Net.Client)                                                                                                                                                  | ≥2.52.0 & < 3.0.0      | source               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `HTTPCLIENT`          | [System.Net.Http.HttpClient](https://docs.microsoft.com/dotnet/api/system.net.http.httpclient) and [System.Net.HttpWebRequest](https://docs.microsoft.com/dotnet/api/system.net.httpwebrequest)                    | *                      | source               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `KAFKA`               | [Confluent.Kafka](https://www.nuget.org/packages/Confluent.Kafka)                                                                                                                                                  | ≥1.4.0 & < 3.0.0 \[5\] | bytecode             | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `MASSTRANSIT`         | [MassTransit](https://www.nuget.org/packages/MassTransit) **Not supported on .NET Framework**                                                                                                                      | ≥8.0.0                 | source               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `MONGODB`             | [MongoDB.Driver.Core](https://www.nuget.org/packages/MongoDB.Driver.Core)                                                                                                                                          | ≥2.13.3 & < 3.0.0      | source & bytecode    | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `MYSQLCONNECTOR`      | [MySqlConnector](https://www.nuget.org/packages/MySqlConnector)                                                                                                                                                    | ≥2.0.0                 | source               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `MYSQLDATA`           | [MySql.Data](https://www.nuget.org/packages/MySql.Data) **Not supported on .NET Framework**                                                                                                                        | ≥8.1.0                 | source               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `NPGSQL`              | [Npgsql](https://www.nuget.org/packages/Npgsql)                                                                                                                                                                    | ≥6.0.0                 | source               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `NSERVICEBUS`         | [NServiceBus](https://www.nuget.org/packages/NServiceBus)                                                                                                                                                          | ≥8.0.0 & < 10.0.0      | source & bytecode    | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `ORACLEMDA`           | [Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core) and [Oracle.ManagedDataAccess](https://www.nuget.org/packages/Oracle.ManagedDataAccess) **Not supported on ARM64**   | ≥23.4.0                | source               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `QUARTZ`              | [Quartz](https://www.nuget.org/packages/Quartz) **Not supported on .NET Framework 4.7.1 and older**                                                                                                                | ≥3.4.0                 | source               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `SQLCLIENT`           | [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient), [System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient) and `System.Data` (shipped with .NET Framework) | * \[6\]                | source               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `STACKEXCHANGEREDIS`  | [StackExchange.Redis](https://www.nuget.org/packages/StackExchange.Redis) **Not supported on .NET Framework**                                                                                                      | ≥2.6.122 & < 3.0.0     | source & bytecode    | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `WCFCLIENT`           | WCF                                                                                                                                                                                                                | *                      | source & bytecode    | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `WCFSERVICE`          | WCF **Not supported on .NET**.                                                                                                                                                                                     | *                      | source & bytecode    | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |

\[1\]: Only integrated pipeline mode is supported.

\[2\]: `ASP.NET (.NET Framework) MVC / WebApi` is not supported on ARM64.

\[3\]: `Azure.` prefixed packages, released after October 1, 2021.

\[4\]: `Elastic.Clients.Elasticsearch` version ≥8.0.0 and <8.10.0.
        Version ≥8.10.0 is supported by `Elastic.Transport` instrumentation.

\[5\]: `Confluent.Kafka` is supported from version ≥1.8.2 on ARM64.

\[6\]: `Microsoft.Data.SqlClient` v3.* is not supported on .NET Framework,
        due to [issue](https://github.com/open-telemetry/opentelemetry-dotnet/issues/4243).
       `System.Data.SqlClient` is supported from version 4.8.5.

### Metrics instrumentations

**Status**: [Mixed](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md).
Metrics are stable, but particular instrumentation are in Experimental status
due to lack of stable semantic convention.

| ID            | Instrumented library                                                                                                                                                                            | Documentation                                                                                                                                                                                                | Supported versions | Instrumentation type | Status                                                                                                                            |
|---------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------|----------------------|-----------------------------------------------------------------------------------------------------------------------------------|
| `ASPNET`      | ASP.NET Framework \[1\] **Not supported on .NET**                                                                                                                                               | [ASP.NET metrics](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/Instrumentation.AspNet-1.9.0-beta.1/src/OpenTelemetry.Instrumentation.AspNet/README.md#list-of-metrics-produced)       | *                  | source & bytecode    | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `ASPNETCORE`  | ASP.NET Core \[2\]  **Not supported on .NET Framework**                                                                                                                                         | [ASP.NET Core metrics](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/Instrumentation.AspNetCore-1.9.0/src/OpenTelemetry.Instrumentation.AspNetCore/README.md#list-of-metrics-produced) | *                  | source               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `HTTPCLIENT`  | [System.Net.Http.HttpClient](https://docs.microsoft.com/dotnet/api/system.net.http.httpclient) and [System.Net.HttpWebRequest](https://docs.microsoft.com/dotnet/api/system.net.httpwebrequest) | [HttpClient metrics](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/Instrumentation.Http-1.9.0/src/OpenTelemetry.Instrumentation.Http/README.md#list-of-metrics-produced)               | *                  | source               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `NETRUNTIME`  | [OpenTelemetry.Instrumentation.Runtime](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Runtime)                                                                                   | [Runtime metrics](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/Instrumentation.Runtime-1.9.0/src/OpenTelemetry.Instrumentation.Runtime/README.md#metrics)                             | *                  | source               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `PROCESS`     | [OpenTelemetry.Instrumentation.Process](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Process)                                                                                   | [Process metrics](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/Instrumentation.Process-0.5.0-beta.6/src/OpenTelemetry.Instrumentation.Process/README.md#metrics)                      | *                  | source               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `NSERVICEBUS` | [NServiceBus](https://www.nuget.org/packages/NServiceBus)                                                                                                                                       | [NServiceBus metrics](https://docs.particular.net/samples/open-telemetry/prometheus-grafana/#reporting-metric-values)                                                                                        | ≥8.0.0 & < 10.0.0  | source & bytecode    | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |

\[1\]: The ASP.NET metrics are generated only if the `AspNet` trace instrumentation
 is also enabled.

\[2\]: This instrumentation automatically enables the
 `Microsoft.AspNetCore.Hosting.HttpRequestIn` spans.

### Logs instrumentations

**Status**: [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md).

| ID        | Instrumented library                                                                                                            | Supported versions | Instrumentation type   | Status                                                                                                                            |
|-----------|---------------------------------------------------------------------------------------------------------------------------------|--------------------|------------------------|-----------------------------------------------------------------------------------------------------------------------------------|
| `ILOGGER` | [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging) **Not supported on .NET Framework** | ≥8.0.0             | bytecode or source [1] | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |

**[1]**: For ASP.NET Core applications, the `LoggingBuilder` instrumentation
can be enabled without using the .NET CLR Profiler by setting
the `ASPNETCORE_HOSTINGSTARTUPASSEMBLIES` environment variable to
`OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper`.

### Instrumentation options

| Environment variable                                                              | Description                                                                                                                                                                                                                                                                                          | Default value | Status                                                                                                                            |
|-----------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------|-----------------------------------------------------------------------------------------------------------------------------------|
| `OTEL_DOTNET_AUTO_ENTITYFRAMEWORKCORE_SET_DBSTATEMENT_FOR_TEXT`                   | Whether the Entity Framework Core instrumentation can pass SQL statements through the `db.statement` attribute. Queries might contain sensitive information. If set to `false`, `db.statement` is recorded only for executing stored procedures.                                                     | `false`       | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_GRAPHQL_SET_DOCUMENT`                                           | Whether the GraphQL instrumentation can pass raw queries through the `graphql.document` attribute. Queries might contain sensitive information.                                                                                                                                                      | `false`       | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_ORACLEMDA_SET_DBSTATEMENT_FOR_TEXT`                             | Whether the Oracle Client instrumentation can pass SQL statements through the `db.statement` attribute. Queries might contain sensitive information. If set to `false`, `db.statement` is recorded only for executing stored procedures.                                                             | `false`       | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_SQLCLIENT_SET_DBSTATEMENT_FOR_TEXT`                             | Whether the SQL Client instrumentation can pass SQL statements through the `db.statement` attribute. Queries might contain sensitive information. If set to `false`, `db.statement` is recorded only for executing stored procedures. **Not supported on .NET Framework for System.Data.SqlClient.** | `false`       | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_TRACES_ASPNET_INSTRUMENTATION_CAPTURE_REQUEST_HEADERS`          | A comma-separated list of HTTP header names. ASP.NET instrumentations will capture HTTP request header values for all configured header names.                                                                                                                                                       |               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_TRACES_ASPNET_INSTRUMENTATION_CAPTURE_RESPONSE_HEADERS`         | A comma-separated list of HTTP header names. ASP.NET instrumentations will capture HTTP response header values for all configured header names. **Not supported on IIS Classic mode.**                                                                                                               |               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_TRACES_ASPNETCORE_INSTRUMENTATION_CAPTURE_REQUEST_HEADERS`      | A comma-separated list of HTTP header names. ASP.NET Core instrumentations will capture HTTP request header values for all configured header names.                                                                                                                                                  |               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_TRACES_ASPNETCORE_INSTRUMENTATION_CAPTURE_RESPONSE_HEADERS`     | A comma-separated list of HTTP header names. ASP.NET Core instrumentations will capture HTTP response header values for all configured header names.                                                                                                                                                 |               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_TRACES_GRPCNETCLIENT_INSTRUMENTATION_CAPTURE_REQUEST_METADATA`  | A comma-separated list of gRPC metadata names. Grpc.Net.Client instrumentations will capture gRPC request metadata values for all configured metadata names.                                                                                                                                         |               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_TRACES_GRPCNETCLIENT_INSTRUMENTATION_CAPTURE_RESPONSE_METADATA` | A comma-separated list of gRPC metadata names. Grpc.Net.Client instrumentations will capture gRPC response metadata values for all configured metadata names.                                                                                                                                        |               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_TRACES_HTTP_INSTRUMENTATION_CAPTURE_REQUEST_HEADERS`            | A comma-separated list of HTTP header names. HTTP Client instrumentations will capture HTTP request header values for all configured header names.                                                                                                                                                   |               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_TRACES_HTTP_INSTRUMENTATION_CAPTURE_RESPONSE_HEADERS`           | A comma-separated list of HTTP header names. HTTP Client instrumentations will capture HTTP response header values for all configured header names.                                                                                                                                                  |               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_EXPERIMENTAL_ASPNETCORE_DISABLE_URL_QUERY_REDACTION`                 | Whether the ASP.NET Core instrumentation turns off redaction of the `url.query` attribute value.                                                                                                                                                                                                     | `false`       | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_EXPERIMENTAL_HTTPCLIENT_DISABLE_URL_QUERY_REDACTION`                 | Whether the HTTP client instrumentation turns off redaction of the `url.full` attribute value.                                                                                                                                                                                                       | `false`       | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_EXPERIMENTAL_ASPNET_DISABLE_URL_QUERY_REDACTION`                     | Whether the ASP.NET instrumentation turns off redaction of the `url.query` attribute value.                                                                                                                                                                                                          | `false`       | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |

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

| Environment variable      | Description                                           | Default value           | Status                                                                                                                      |
|---------------------------|-------------------------------------------------------|-------------------------|-----------------------------------------------------------------------------------------------------------------------------|
| `OTEL_TRACES_SAMPLER`     | Sampler to be used for traces \[1\]                   | `parentbased_always_on` | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_TRACES_SAMPLER_ARG` | String value to be used as the sampler argument \[2\] |                         | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |

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

| Environment variable    | Description                                                                                       | Default value | Status                                                                                                                      |
|-------------------------|---------------------------------------------------------------------------------------------------|---------------|-----------------------------------------------------------------------------------------------------------------------------|
| `OTEL_TRACES_EXPORTER`  | Comma-separated list of propagators. Supported options: `otlp`, `zipkin`, `console`, `none`. See [the OpenTelemetry specification](https://github.com/open-telemetry/opentelemetry-specification/blob/v1.35.0/specification/configuration/sdk-environment-variables.md#exporter-selection) for more details.| `otlp`        | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_METRICS_EXPORTER`  | Comma-separated list of propagators. Supported options: `otlp`, `prometheus`, `console`, `none`. See [the OpenTelemetry specification](https://github.com/open-telemetry/opentelemetry-specification/blob/v1.35.0/specification/configuration/sdk-environment-variables.md#exporter-selection) for more details. | `otlp`        | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_LOGS_EXPORTER`     | Comma-separated list of propagators. Supported options: `otlp`, `console`, `none`. See [the OpenTelemetry specification](https://github.com/open-telemetry/opentelemetry-specification/blob/v1.35.0/specification/configuration/sdk-environment-variables.md#exporter-selection) for more details.| `otlp`        | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |

### Traces exporter

| Environment variable             | Description                                                                  | Default value | Status                                                                                                                      |
|----------------------------------|------------------------------------------------------------------------------|---------------|-----------------------------------------------------------------------------------------------------------------------------|
| `OTEL_BSP_SCHEDULE_DELAY`        | Delay interval (in milliseconds) between two consecutive exports.            | `5000`        | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_BSP_EXPORT_TIMEOUT`        | Maximum allowed time (in milliseconds) to export data                        | `30000`       | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_BSP_MAX_QUEUE_SIZE`        | Maximum queue size.                                                          | `2048`        | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_BSP_MAX_EXPORT_BATCH_SIZE` | Maximum batch size. Must be less than or equal to `OTEL_BSP_MAX_QUEUE_SIZE`. | `512`         | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |

### Metrics exporter

| Environment variable          | Description                                                                   | Default value                                           | Status                                                                                                                      |
|-------------------------------|-------------------------------------------------------------------------------|---------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------|
| `OTEL_METRIC_EXPORT_INTERVAL` | The time interval (in milliseconds) between the start of two export attempts. | `60000` for OTLP exporter, `10000` for console exporter | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_METRIC_EXPORT_TIMEOUT`  | Maximum allowed time (in milliseconds) to export data.                        | `30000` for OTLP exporter, none for console exporter    | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |

### Logs exporter

| Environment variable                              | Description                                             | Default value | Status                                                                                                                            |
|---------------------------------------------------|---------------------------------------------------------|---------------|-----------------------------------------------------------------------------------------------------------------------------------|
| `OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE` | Whether the formatted log message should be set or not. | `false`       | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |

### OTLP

**Status**: [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md)

To enable the OTLP exporter, set the `OTEL_TRACES_EXPORTER`/`OTEL_METRICS_EXPORTER`/`OTEL_LOGS_EXPORTER`
environment variable to `otlp`.

To customize the OTLP exporter using environment variables, see the
[OTLP exporter documentation](https://github.com/open-telemetry/opentelemetry-dotnet/tree/core-1.7.0/src/OpenTelemetry.Exporter.OpenTelemetryProtocol#environment-variables).
Important environment variables include:

| Environment variable                     | Description                                                                                                                                                                                                | Default value                                                                                             | Status                                                                                                                      |
|------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------|
| `OTEL_EXPORTER_OTLP_ENDPOINT`            | Target endpoint for the OTLP exporter. See [the OpenTelemetry specification](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/exporter.md) for more details. | `http://localhost:4318` for the `http/protobuf` protocol, `http://localhost:4317` for the `grpc` protocol | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_EXPORTER_OTLP_PROTOCOL`            | OTLP exporter transport protocol. Supported values are `grpc`, `http/protobuf`. [1]                                                                                                                        | `http/protobuf`                                                                                           | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_EXPORTER_OTLP_TIMEOUT`             | The max waiting time (in milliseconds) for the backend to process each batch.                                                                                                                              | `10000`                                                                                                   | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_EXPORTER_OTLP_HEADERS`             | Comma-separated list of additional HTTP headers sent with each export, for example: `Authorization=secret,X-Key=Value`.                                                                                    |                                                                                                           | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT`      | Maximum allowed attribute value size.                                                                                                                                                                      | none                                                                                                      | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_ATTRIBUTE_COUNT_LIMIT`             | Maximum allowed span attribute count.                                                                                                                                                                      | 128                                                                                                       | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_SPAN_ATTRIBUTE_VALUE_LENGTH_LIMIT` | Maximum allowed attribute value size. [Not applicable for metrics.](https://github.com/open-telemetry/opentelemetry-specification/blob/v1.15.0/specification/metrics/sdk.md#attribute-limits).             | none                                                                                                      | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_SPAN_ATTRIBUTE_COUNT_LIMIT`        | Maximum allowed span attribute count. [Not applicable for metrics.](https://github.com/open-telemetry/opentelemetry-specification/blob/v1.15.0/specification/metrics/sdk.md#attribute-limits).             | 128                                                                                                       | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_SPAN_EVENT_COUNT_LIMIT`            | Maximum allowed span event count.                                                                                                                                                                          | 128                                                                                                       | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_SPAN_LINK_COUNT_LIMIT`             | Maximum allowed span link count.                                                                                                                                                                           | 128                                                                                                       | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_EVENT_ATTRIBUTE_COUNT_LIMIT`       | Maximum allowed attribute per span event count.                                                                                                                                                            | 128                                                                                                       | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_LINK_ATTRIBUTE_COUNT_LIMIT`        | Maximum allowed attribute per span link count.                                                                                                                                                             | 128                                                                                                       | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE`        | The aggregation temporality to use on the basis of instrument kind.                                                                                                                                                             | `cumulative`                                                                                                       | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |

**[1]**: Considerations on the `OTEL_EXPORTER_OTLP_PROTOCOL`:

- The OpenTelemetry .NET Automatic Instrumentation defaults to `http/protobuf`,
  which differs from the OpenTelemetry .NET SDK default value of `grpc`.
- On .NET 6 and higher, the application must reference [`Grpc.Net.Client`](https://www.nuget.org/packages/Grpc.Net.Client/)
  to use the `grpc` OTLP exporter protocol. For example, by adding
  `<PackageReference Include="Grpc.Net.Client" Version="2.64.0" />` to the
  `.csproj` file.
- On .NET Framework, the `grpc` OTLP exporter protocol is not supported.

**[2]**: The recognized (case-insensitive) values for `OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE` are:

- `Cumulative`: Choose cumulative aggregation temporality for all instrument kinds.
- `Delta`: Choose Delta aggregation temporality for Counter, Asynchronous Counter and Histogram instrument kinds, choose Cumulative aggregation for UpDownCounter and Asynchronous UpDownCounter instrument kinds.
> [!CAUTION] 
> **Currently is not working**
> 
> `LowMemory`: This configuration uses Delta aggregation temporality for Synchronous Counter and Histogram and uses Cumulative aggregation temporality for Synchronous UpDownCounter, Asynchronous Counter, and Asynchronous UpDownCounter instrument kinds.

### Prometheus

**Status**: [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md)

> [!WARNING]
> **Do NOT use in production.**
>
> Prometheus exporter is intended for the inner dev loop.
> Production environments can use a combination of OTLP exporter
> with [OpenTelemetry Collector](https://github.com/open-telemetry/opentelemetry-collector-releases)
> having [`otlp` receiver](https://github.com/open-telemetry/opentelemetry-collector/tree/v0.97.0/receiver/otlpreceiver)
> and [`prometheus` exporter](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/v0.97.0/exporter/prometheusexporter).

To enable the Prometheus exporter, set the `OTEL_METRICS_EXPORTER` environment
variable to `prometheus`.

The exporter exposes the metrics HTTP endpoint on `http://localhost:9464/metrics`
and it caches the responses for 300 milliseconds.

See the
[Prometheus Exporter HttpListener documentation](https://github.com/open-telemetry/opentelemetry-dotnet/tree/coreunstable-1.9.0-beta.2/src/OpenTelemetry.Exporter.Prometheus.HttpListener).
to learn more.

### Zipkin

**Status**: [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md)

To enable the Zipkin exporter, set the `OTEL_TRACES_EXPORTER` environment
variable to `zipkin`.

To customize the Zipkin exporter using environment variables,
see the [Zipkin exporter documentation](https://github.com/open-telemetry/opentelemetry-dotnet/tree/core-1.9.0/src/OpenTelemetry.Exporter.Zipkin#configuration-using-environment-variables).
Important environment variables include:

| Environment variable            | Description | Default value                        | Status                                                                                                                      |
|---------------------------------|-------------|--------------------------------------|-----------------------------------------------------------------------------------------------------------------------------|
| `OTEL_EXPORTER_ZIPKIN_ENDPOINT` | Zipkin URL  | `http://localhost:9411/api/v2/spans` | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |

## Additional settings

| Environment variable                                | Description                                                                                                                                                                                                                                                                                                                                                                                        | Default value | Status                                                                                                                            |
|-----------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------|-----------------------------------------------------------------------------------------------------------------------------------|
| `OTEL_DOTNET_AUTO_TRACES_ENABLED`                   | Enables traces.                                                                                                                                                                                                                                                                                                                                                                                    | `true`        | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_OPENTRACING_ENABLED`              | Enables OpenTracing tracer.                                                                                                                                                                                                                                                                                                                                                                        | `false`       | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_LOGS_ENABLED`                     | Enables logs.                                                                                                                                                                                                                                                                                                                                                                                      | `true`        | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_METRICS_ENABLED`                  | Enables metrics.                                                                                                                                                                                                                                                                                                                                                                                   | `true`        | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_NETFX_REDIRECT_ENABLED`           | Enables automatic redirection of the assemblies used by the automatic instrumentation on the .NET Framework.                                                                                                                                                                                                                                                                                       | `true`        | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES`        | Comma-separated list of additional `System.Diagnostics.ActivitySource` names to be added to the tracer at the startup. Use it to capture manually instrumented spans.                                                                                                                                                                                                                              |               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_LEGACY_SOURCES` | Comma-separated list of additional legacy source names to be added to the tracer at the startup. Use it to capture `System.Diagnostics.Activity` objects created without using the `System.Diagnostics.ActivitySource` API.                                                                                                                                                                        |               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_FLUSH_ON_UNHANDLEDEXCEPTION`      | Controls whether the telemetry data is flushed when an [AppDomain.UnhandledException](https://docs.microsoft.com/en-us/dotnet/api/system.appdomain.unhandledexception) event is raised. Set to `true` when you suspect that you are experiencing a problem with missing telemetry data and also experiencing unhandled exceptions.                                                                 | `false`       | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES`       | Comma-separated list of additional `System.Diagnostics.Metrics.Meter` names to be added to the meter at the startup. Use it to capture manually created metrics.                                                                                                                                                                                                                                   |               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_PLUGINS`                          | Colon-separated list of OTel SDK instrumentation plugin types, specified with the [assembly-qualified name](https://docs.microsoft.com/en-us/dotnet/api/system.type.assemblyqualifiedname?view=net-6.0#system-type-assemblyqualifiedname). _Note: This list must be colon-separated because the type names may include commas._ See more info on how to write plugins at [plugins.md](plugins.md). |               | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |

## RuleEngine

RuleEngine is a feature that validates OpenTelemetry API, SDK,
Instrumentation, and Exporter assemblies for unsupported scenarios,
ensuring that OpenTelemetry automatic instrumentation is more
stable by backing of instead of crashing. It works on .NET 6 and higher.

Enable RuleEngine only during the first run of the application,
or when the deployment changes or the Automatic Instrumentation
library is upgraded. Once validated, there's no need to revalidate
the rules when the application restarts.

| Environment variable                   | Description         | Default value | Status                                                                                                                            |
|----------------------------------------|---------------------|---------------|-----------------------------------------------------------------------------------------------------------------------------------|
| `OTEL_DOTNET_AUTO_RULE_ENGINE_ENABLED` | Enables RuleEngine. | `true`        | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |

## .NET CLR Profiler

The CLR uses the following
environment variables to set up the profiler. See
[.NET Runtime Profiler Loading](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/profiling/Profiler%20Loading.md)
for more information.

| .NET Framework environment variable | .NET environment variable  | Description                                                                             | Required value                                                                                                                                                                                                                                                  | Status                                                                                                                            |
|-------------------------------------|----------------------------|-----------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------|
| `COR_ENABLE_PROFILING`              | `CORECLR_ENABLE_PROFILING` | Enables the profiler.                                                                   | `1`                                                                                                                                                                                                                                                             | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `COR_PROFILER`                      | `CORECLR_PROFILER`         | CLSID of the profiler.                                                                  | `{918728DD-259F-4A6A-AC2B-B85E1B658318}`                                                                                                                                                                                                                        | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `COR_PROFILER_PATH`                 | `CORECLR_PROFILER_PATH`    | Path to the profiler.                                                                   | `$INSTALL_DIR/linux-x64/OpenTelemetry.AutoInstrumentation.Native.so` for Linux glibc, `$INSTALL_DIR/linux-musl-x64/OpenTelemetry.AutoInstrumentation.Native.so` for Linux musl, `$INSTALL_DIR/osx-x64/OpenTelemetry.AutoInstrumentation.Native.dylib` for macOS | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `COR_PROFILER_PATH_32`              | `CORECLR_PROFILER_PATH_32` | Path to the 32-bit profiler. Bitness-specific paths take precedence over generic paths. | `$INSTALL_DIR/win-x86/OpenTelemetry.AutoInstrumentation.Native.dll` for Windows                                                                                                                                                                                 | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `COR_PROFILER_PATH_64`              | `CORECLR_PROFILER_PATH_64` | Path to the 64-bit profiler. Bitness-specific paths take precedence over generic paths. | `$INSTALL_DIR/win-x64/OpenTelemetry.AutoInstrumentation.Native.dll` for Windows                                                                                                                                                                                 | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |

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
```

## .NET Runtime

On .NET it is required to set the
[`DOTNET_STARTUP_HOOKS`](https://github.com/dotnet/runtime/blob/main/docs/design/features/host-startup-hook.md)
environment variable.

The [`DOTNET_ADDITIONAL_DEPS`](https://github.com/dotnet/runtime/blob/main/docs/design/features/additional-deps.md)
and [`DOTNET_SHARED_STORE`](https://docs.microsoft.com/en-us/dotnet/core/deploying/runtime-store)
environment variable are used to mitigate assembly version conflicts in .NET.

| Environment variable     | Required value                                                       | Status                                                                                                                            |
|--------------------------|----------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------|
| `DOTNET_STARTUP_HOOKS`   | `$INSTALL_DIR/net/OpenTelemetry.AutoInstrumentation.StartupHook.dll` | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `DOTNET_ADDITIONAL_DEPS` | `$INSTALL_DIR/AdditionalDeps`                                        | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `DOTNET_SHARED_STORE`    | `$INSTALL_DIR/store`                                                 | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |

## Internal logs

The default directory paths for internal logs are:

- Windows: `%ProgramData%\OpenTelemetry .NET AutoInstrumentation\logs`
- Linux: `/var/log/opentelemetry/dotnet`
- macOS: `/var/log/opentelemetry/dotnet`

If the default log directories can't be created,
the instrumentation uses the path of the current user's [temporary folder](https://docs.microsoft.com/en-us/dotnet/api/System.IO.Path.GetTempPath?view=net-6.0)
instead.

| Environment variable                                | Description                                                             | Default value                            | Status                                                                                                                            |
|-----------------------------------------------------|-------------------------------------------------------------------------|------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------|
| `OTEL_DOTNET_AUTO_LOG_DIRECTORY`                    | Directory of the .NET Tracer logs.                                      | _See the previous note on default paths_ | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_LOG_LEVEL`                                    | SDK log level. (supported values: `none`,`error`,`warn`,`info`,`debug`) | `info`                                   | [Stable](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md)       |
| `OTEL_DOTNET_AUTO_TRACES_CONSOLE_EXPORTER_ENABLED`  | Whether the traces console exporter is enabled or not.                  | `false`                                  | [Deprecated](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_METRICS_CONSOLE_EXPORTER_ENABLED` | Whether the metrics console exporter is enabled or not.                 | `false`                                  | [Deprecated](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_LOGS_CONSOLE_EXPORTER_ENABLED`    | Whether the logs console exporter is enabled or not.                    | `false`                                  | [Deprecated](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
| `OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE`   | Whether the log state should be formatted.                              | `false`                                  | [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md) |
