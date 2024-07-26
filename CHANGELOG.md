# Changelog

All notable changes to this component are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
This component adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/compare/v1.7.0..HEAD)

### Added

- Support for Operating System resource detector.
- Added support for OTEL_TRACES_EXPORTER, OTEL_METRICS_EXPORTER, OTEL_LOGS_EXPORTER
  to handle comma-separated list.
- The environment variables `OTEL_TRACES_EXPORTER`, `OTEL_METRICS_EXPORTER`,
  and `OTEL_LOGS_EXPORTER` now support configuring console exporters for traces,
  metrics, and logs, respectively.
- Environment variables `OTEL_DOTNET_AUTO_TRACES_CONSOLE_EXPORTER_ENABLED`,
  `OTEL_DOTNET_AUTO_METRICS_CONSOLE_EXPORTER_ENABLED`, and
  `OTEL_DOTNET_AUTO_LOGS_CONSOLE_EXPORTER_ENABLED` are now marked as deprecated.

### Changed

- Referencing `OpenTelemetry.AutoInstrumentation` manually no longer visibly injects
  instrumentation scripts into projects in an editor's solution window.

#### Dependency updates

- Following packages updated
  - `OpenTelemetry.Exporter.Prometheus.HttpListener` from `1.9.0-beta.1` to `1.9.0-beta.2`,
  - `OpenTelemetry.Shims.OpenTracing` from `1.9.0-beta.1` to `1.9.0-beta.2`.
- .NET only, following packages updated
  - `OpenTelemetry.Instrumentation.StackExchangeRedis` from `1.0.0-rc9.15` to `1.9.0-beta.1`.
- .NET Framework only, following packages updated
  - `Google.Protobuf` updated from `3.27.1` to `3.27.2`,
  - `Grpc.Core.Api` from `2.63.0` to `2.64.0`,
  - `Microsoft.Extensions.Configuration.Binder` from `8.0.1` to `8.0.2`,
  - `System.Text.Json` from `8.0.3` to `8.0.4`.

### Deprecated

### Removed

- Support for macOS Big Sur 11 x64.
  macOs libraries are built and tested against [macOS Monterey 12 x64](https://github.com/actions/runner-images/blob/main/images/macos/macos-12-Readme.md).
- Support for StackExchange.Redis < 2.6.122.

### Fixed

## [1.7.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.7.0)

- [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.9.0`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.9.0)
- `System.Diagnostics.DiagnosticSource`: [`8.0.0`](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/8.0.0)

### Added

- Support for capturing HTTP headers for following traces instrumentations:
  - ASP.NET, configurable by
    `OTEL_DOTNET_AUTO_TRACES_ASPNET_INSTRUMENTATION_CAPTURE_REQUEST_HEADERS`
    and `OTEL_DOTNET_AUTO_TRACES_ASPNET_INSTRUMENTATION_CAPTURE_RESPONSE_HEADERS`,
  - ASP.NET Core, configurable by
    `OTEL_DOTNET_AUTO_TRACES_ASPNETCORE_INSTRUMENTATION_CAPTURE_REQUEST_HEADERS`
    and `OTEL_DOTNET_AUTO_TRACES_ASPNETCORE_INSTRUMENTATION_CAPTURE_RESPONSE_HEADERS`,
  - HTTP Client, configurable by
    `OTEL_DOTNET_AUTO_TRACES_HTTP_INSTRUMENTATION_CAPTURE_REQUEST_HEADERS`
    and `OTEL_DOTNET_AUTO_TRACES_HTTP_INSTRUMENTATION_CAPTURE_RESPONSE_HEADERS`.
- Support for capturing gRPC metadata for Grpc.Net.Client traces instrumentation.
  It can by configured by
  `OTEL_DOTNET_AUTO_TRACES_GRPCNETCLIENT_INSTRUMENTATION_CAPTURE_REQUEST_METADATA`
  and `OTEL_DOTNET_AUTO_TRACES_GRPCNETCLIENT_INSTRUMENTATION_CAPTURE_RESPONSE_METADATA`.
- Support for [Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core)
  and [Oracle.ManagedDataAccess](https://www.nuget.org/packages/Oracle.ManagedDataAccess)
  traces instrumentation from 23.4.0 together with support for
  `OTEL_DOTNET_AUTO_ORACLEMDA_SET_DBSTATEMENT_FOR_TEXT` environment variable.
  ARM64 platform is not supported.
- Add support for NServiceBus 9.x metrics and traces instrumentations.

### Changed

- Musl-based (Alpine) libraries are compiled on Alpine v3.19.
- Do not use message creation context as a parent for consumer spans for `Confluent.Kafka`
  client instrumentation. See the [issue](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/3434)
  for details.
- Do not create consumer spans related to `PartitionEOF` events
  for `Confluent.Kafka` client instrumentation.
- Following properties can be set before calling plugins:
  - `OpenTelemetry.Instrumentation.AspNetCore.AspNetCoreTraceInstrumentationOptions.EnrichWithHttpRequest`,
  - `OpenTelemetry.Instrumentation.AspNetCore.AspNetCoreTraceInstrumentationOptions.EnrichWithHttpResponse`,
  - `OpenTelemetry.Instrumentation.AspNet.AspNetTraceInstrumentationOptions.EnrichWithHttpRequest`,
  - `OpenTelemetry.Instrumentation.AspNet.AspNetTraceInstrumentationOptions.EnrichWithHttpResponse`,
  - `OpenTelemetry.Instrumentation.GrpcNetClient.GrpcClientTraceInstrumentationOptions.EnrichWithHttpRequestMessage`,
  - `OpenTelemetry.Instrumentation.GrpcNetClient.GrpcClientTraceInstrumentationOptions.EnrichWithHttpResponseMessage`,
  - `OpenTelemetry.Instrumentation.Http.HttpClientTraceInstrumentationOptions.EnrichWithHttpRequestMessage`,
  - `OpenTelemetry.Instrumentation.Http.HttpClientTraceInstrumentationOptions.EnrichWithHttpWebRequest`,
  - `OpenTelemetry.Instrumentation.Http.HttpClientTraceInstrumentationOptions.EnrichWithHttpResponseMessage`,
  - `OpenTelemetry.Instrumentation.Http.HttpClientTraceInstrumentationOptions.EnrichWithHttpWebResponse`.

#### Dependency updates

- Updated [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.9.0`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.9.0).
- Following packages updated
  - `OpenTelemetry.Exporter.Prometheus.HttpListener` from `1.8.0-rc.1` to `1.9.0-beta.1`,
  - `OpenTelemetry.Instrumentation.GrpcNetClient` from `1.8.0-beta.1` to `1.9.0-beta.1`,
  - `OpenTelemetry.Instrumentation.Http` from `1.8.1` to `1.9.0`,
  - `OpenTelemetry.Instrumentation.Process` from `0.5.0-beta.5` to `0.5.0-beta.6`,
  - `OpenTelemetry.Instrumentation.Quartz` from `1.0.0-beta.2` to `1.0.0-beta.3`,
  - `OpenTelemetry.Instrumentation.Runtime` from `1.8.0` to `1.9.0`,
  - `OpenTelemetry.Instrumentation.SqlClient` from `1.8.0-beta.1` to `1.9.0-beta.1`,
  - `OpenTelemetry.Instrumentation.StackExchangeRedis` from `1.0.0-rc9.14` to `1.0.0-rc9.15`,
  - `OpenTelemetry.Instrumentation.Wcf` from `1.0.0-rc.16` to `1.0.0-rc.17`,
  - `OpenTelemetry.Shims.OpenTracing` from `1.7.0-beta.1` to `1.9.0-beta.1`,
  - `OpenTelemetry.Resources.Azure` from `1.0.0-beta.7` to `1.0.0-beta.8`,
  - `OpenTelemetry.Resources.Host` from `0.1.0-beta.1` to `0.1.0-beta.2`.
  - `OpenTelemetry.Resources.Process` from `0.1.0-beta.1` to `0.1.0-beta.2`.
  - `OpenTelemetry.Resources.ProcessRuntime` from `0.1.0-beta.1` to `0.1.0-beta.2`.
- .NET only, following packages updated
  - `OpenTelemetry.Instrumentation.AspNetCore` from `1.8.1` to `1.9.0`,
  - `OpenTelemetry.Instrumentation.EntityFrameworkCore` from `1.0.0-beta.11` to `1.0.0-beta.12`,
  - `OpenTelemetry.Resources.Container` from `1.0.0-beta.7` to `1.0.0-beta.9`.
- .NET Framework only, following packages updated
  - `OpenTelemetry.Instrumentation.AspNet` from `1.8.0-beta.2` to `1.9.0-beta.1`.

### Deprecated

- Support for macOS Big Sur 11 x64.
  All further releases will be supporting [macOS Monterey 12 x64](https://github.com/actions/runner-images/blob/main/images/macos/macos-12-Readme.md)
  and newer.

## [1.6.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.6.0)

- [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.8.1`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.8.1)
- `System.Diagnostics.DiagnosticSource`: [`8.0.0`](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/8.0.0)

### Changed

- To prevent sensitive information from leaking through query strings, the
  following instrumentations redact by default any value detected in query string
  components when building the `url.query` or the `url.full` attributes:
  `OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Instrumentation.AspNetCore`,
  `OpenTelemetry.Instrumentation.AspNet`. For example, `?key1=value1&key2=value2`
  becomes `?key1=Redacted&key2=Redacted`. You can customize this behavior through
  the environment variables. See the
  [instrumentation options](./docs/config.md#instrumentation-options) table for details.

#### Dependency updates

- Following packages updated
  - `OpenTelemetry.Instrumentation.Http` from `1.8.0` to `1.8.1`.
- Following packages replaced
  - `OpenTelemetry.ResourceDetectors.Azure` version `1.0.0-beta.6`
    by `OpenTelemetry.Resources.Azure` version `1.0.0-beta.7`,
  - `OpenTelemetry.ResourceDetectors.Host` version `0.1.0-alpha.3`
    by `OpenTelemetry.Resources.Host` version `0.1.0-beta.1`,
  - `OpenTelemetry.ResourceDetectors.Process` version `0.1.0-alpha.3`
    by `OpenTelemetry.Resources.Process` version `0.1.0-beta.1`,
  - `OpenTelemetry.ResourceDetectors.ProcessRuntime` version `0.1.0-alpha.3`
    by `OpenTelemetry.Resources.ProcessRuntime` version `0.1.0-beta.1`.
- .NET only, following packages updated
  - `Microsoft.Extensions.Configuration.Binder` from `8.0.0` to `8.0.1`,
  - `OpenTelemetry.Instrumentation.AspNetCore` from `1.8.0` to `1.8.1`.
- .NET only, following packages replaced:
  - `OpenTelemetry.ResourceDetectors.Container` version `1.0.0-beta.7`
    by `OpenTelemetry.Resources.Container` version `1.0.0-beta.8`.
- .NET Framework only, following packages updated
  - `Grpc.Core.Api` from `2.62.0` to `2.63.0`,
  - `OpenTelemetry.Instrumentation.AspNet` from `1.8.0-beta.1` to `1.8.0-beta.2`.

### Fixed

- Stop creating `receive` consumer spans for consume attempts that returned no message.
  For details, see [#3367](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/3367)

## [1.5.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.5.0)

- Updated [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.8.1`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.8.1).
- [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.8.0`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.8.0)
- `System.Diagnostics.DiagnosticSource`: [`8.0.0`](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/8.0.0)

### Added

- .NET only, warning in logs about End of Support date
  and upcoming End of Support date for .NET version.
- Experimental support for ARM64 on Ubuntu, Alpine and Debian based images.
- Experimental ARM64 support for the `OpenTelemetry.AutoInstrumentation` NuGet package.

### Changed

- Changed supported method parameters for plugins
  - from `OpenTelemetry.Instrumentation.AspNet.AspNetInstrumentationOptions`
    to `OpenTelemetry.Instrumentation.AspNet.AspNetTraceInstrumentationOptions`.

#### Dependency updates

- Updated [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.8.0`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.8.0).
- Following packages updated
  - `MongoDB.Driver.Core.Extensions.DiagnosticSources` from `1.3.0` to `1.4.0`.
  - `OpenTelemetry.Exporter.Prometheus.HttpListener` from `1.7.0-rc.1` to `1.8.0-rc.1`,
  - `OpenTelemetry.Instrumentation.Http` from `1.7.1` to `1.8.0`,
  - `OpenTelemetry.Instrumentation.Process` from `0.5.0-beta.4` to `0.5.0-beta.5`,
  - `OpenTelemetry.Instrumentation.Quartz` from `1.0.0-beta.1` to `1.0.0-beta.2`,
  - `OpenTelemetry.Instrumentation.Runtime` from `1.7.0` to `1.8.0`,
  - `OpenTelemetry.Instrumentation.SqlClient` from `1.7.0-beta.1` to `1.8.0-beta.1`,
  - `OpenTelemetry.Instrumentation.StackExchangeRedis` from `1.0.0-rc9.13` to `1.0.0-rc9.14`,
  - `OpenTelemetry.Instrumentation.Wcf` from `1.0.0-rc.15` to `1.0.0-rc.16`,
  - `OpenTelemetry.ResourceDetectors.Azure` from `1.0.0-beta.5` to `1.0.0-beta.6`,
  - `OpenTelemetry.ResourceDetectors.Host` from `0.1.0-alpha.2` to `0.1.0-alpha.3`.
  - `OpenTelemetry.ResourceDetectors.Process` from `0.1.0-alpha.2` to `0.1.0-alpha.3`.
  - `OpenTelemetry.ResourceDetectors.ProcessRuntime` from `0.1.0-alpha.2` to `0.1.0-alpha.3`.
- .NET only, following packages updated
  - `OpenTelemetry.Instrumentation.AspNetCore` from `1.7.1` to `1.8.0`,
  - `OpenTelemetry.Instrumentation.EntityFrameworkCore` from `1.0.0-beta.10` to `1.0.0-beta.11`,
  - `OpenTelemetry.ResourceDetectors.Container` from `1.0.0-beta.6` to `1.0.0-beta.7`.
- .NET Framework only, following packages updated
  - `Google.Protobuf` updated from `3.25.2` to `3.27.1`,
  - `Grpc.Core.Api` from `2.60.0` to `2.62.0`,
  - `Microsoft.Extensions.DependencyInjection.Abstractions` from `8.0.0` to `8.0.1`,
  - `Microsoft.Extensions.Options` from `8.0.1` to `8.0.2`,
  - `Microsoft.Extensions.Logging.Abstractions` from `8.0.0` to `8.0.1`,
  - `OpenTelemetry.Instrumentation.AspNet` from `1.7.0-beta.2` to `1.8.0-beta.1`,
  - `System.Text.Json` from `8.0.1` to `8.0.3`.

### Fixed

- Resolved a crash issue caused by `System.IO.FileLoadException` for
  `Microsoft.Extensions*.dll` libraries. This issue was due to a conflict with
  runtime store libraries, impacting applications with mismatched dependency
  versions. This fix enhances stability by addressing the underlying
  compatibility concerns. For details see:
  ([#3075](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/3075),
  [#3168](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/3168))

## [1.4.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.4.0)

### Added

- Support for `OTEL_DOTNET_AUTO_SQLCLIENT_SET_DBSTATEMENT_FOR_TEXT`
  and `OTEL_DOTNET_AUTO_ENTITYFRAMEWORKCORE_SET_DBSTATEMENT_FOR_TEXT`
  environment variables which controls whether the `db.statement`
  attribute is set for SQL statements for SQL Client and Entity Framework Core
  instrumentations. The default value for both environment variables is `false`
  due to the risk of leaking sensitive information in the collected database queries.

### Changed

- Changed supported method parameters for plugins
  - from `OpenTelemetry.Instrumentation.GrpcNetClient.GrpcClientInstrumentationOptions`
    to `OpenTelemetry.Instrumentation.GrpcNetClient.GrpcClientTraceInstrumentationOptions`,
  - from `OpenTelemetry.Instrumentation.SqlClient.SqlClientInstrumentationOptions`
    to `OpenTelemetry.Instrumentation.SqlClient.SqlClientTraceInstrumentationOptions`.

#### Dependency updates

- Following packages updated
  - `OpenTelemetry.ResourceDetectors.Azure` from `1.0.0-beta.4` to `1.0.0-beta.5`,
  - `OpenTelemetry.ResourceDetectors.Container` from `1.0.0-beta.5` to `1.0.0-beta.6`.
  - `OpenTelemetry.Instrumentation.Http` from `1.7.0` to `1.7.1`,
  - `OpenTelemetry.Instrumentation.SqlClient` from `1.6.0-beta.3` to `1.7.0-beta.1`,
  - `OpenTelemetry.Instrumentation.Wcf` from `1.0.0-rc.14` to `1.0.0-rc.15`,
- .NET only, following packages updated
  - `OpenTelemetry.Instrumentation.AspNetCore` from `1.7.0` to `1.7.1`,
  - `OpenTelemetry.Instrumentation.EntityFrameworkCore` from `1.0.0-beta.9` to `1.0.0-beta.10`,
  - `OpenTelemetry.Instrumentation.GrpcNetClient` from `1.6.0-beta.3` to `1.7.0-beta.1`.
- .NET Framework only, following packages updated
  - `OpenTelemetry.Instrumentation.AspNet` from `1.7.0-beta.1` to `1.7.0-beta.2`.

### Removed

- Container resource detector will be not executed on .NET Framework.
  It was not giving any results before changes.

### Fixed

- Fix ASP.NET Core traces instrumentation for .NET7. It is now using native
  support by `Microsoft.AspNetCore` instead of `OpenTelemetry.Instrumentation.AspNetCore`.

## [1.3.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.3.0)

- [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.7.0`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.7.0)
- `System.Diagnostics.DiagnosticSource`: [`8.0.0`](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/8.0.0)

### Added

- Support for Host, Process, and Process Runtime resource detectors.
- Support for `OpenTelemetry.Instrumentation.AspNet.AspNetMetricsInstrumentationOptions`
  for plugins.
- Support for [Confluent.Kafka](https://www.nuget.org/packages/Confluent.Kafka)
  traces instrumentation from 1.4.0 to 3.0.0 (excluding).

### Changed

- Changed minimal supported version of `Microsoft.Extensions.Logging`
  for `ILOGGER` instrumentation from `6.0.0` to `8.0.0`.
- Changed supported method parameters for plugins
  - from `OpenTelemetry.Instrumentation.AspNetCore.AspNetCoreInstrumentationOptions`
    to `OpenTelemetry.Instrumentation.AspNetCore.AspNetCoreTraceInstrumentationOptions`,
  - from `OpenTelemetry.Instrumentation.Http.HttpClientInstrumentationOptions`
    to `OpenTelemetry.Instrumentation.Http.HttpClientTraceInstrumentationOptions`.

#### Dependency updates

- Updated [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.7.0`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.7.0).
- Following packages updated
  - `OpenTelemetry.Exporter.Prometheus.HttpListener` from `1.6.0-rc.1` to `1.7.0-rc.1`,
  - `OpenTelemetry.Instrumentation.Http` from `1.6.0` to `1.7.0`,
  - `OpenTelemetry.Instrumentation.Process` from `0.5.0-beta.3` to `0.5.0-beta.4`,
  - `OpenTelemetry.Instrumentation.Quartz` from `1.0.0-alpha.3` to `1.0.0-beta.1`,
  - `OpenTelemetry.Instrumentation.Runtime` from `1.5.1` to `1.7.0`,
  - `OpenTelemetry.Instrumentation.SqlClient` from `1.6.0-beta.2` to `1.6.0-beta.3`,
  - `OpenTelemetry.Instrumentation.StackExchangeRedis` from `1.0.0-rc9.12` to `1.0.0-rc9.13`,
  - `OpenTelemetry.Instrumentation.Wcf` from `1.0.0-rc.13` to `1.0.0-rc.14`,
  - `OpenTelemetry.Shims.OpenTracing` from `1.6.0-beta.1` to `1.7.0-beta.1`,
  - `OpenTelemetry.ResourceDetectors.Azure` from `1.0.0-beta.3` to `1.0.0-beta.4`,
  - `OpenTelemetry.ResourceDetectors.Container` from `1.0.0-beta.4` to `1.0.0-beta.5`,
  - `OpenTelemetry.ResourceDetectors.ProcessRuntime` from `0.1.0-alpha.1` to `0.1.0-alpha.2`.
- .NET only, following packages updated
  - `Google.Protobuf` updated from `3.19.4` to `3.22.5`.
  - `Microsoft.Extensions.Configuration` from `3.1.0` to `8.0.0`,
  - `Microsoft.Extensions.Configuration.Abstractions` from `3.1.0` to `8.0.0`,
  - `Microsoft.Extensions.Configuration.Binder` from `3.1.0` to `8.0.0`,
  - `Microsoft.Extensions.DependencyInjection` from `3.1.0` to `8.0.0`,
  - `Microsoft.Extensions.DependencyInjection.Abstractions` from `3.1.0` to `8.0.0`,
  - `Microsoft.Extensions.Logging` from `6.0.0` to `8.0.0`,
  - `Microsoft.Extensions.Options` from `3.1.0` to `8.0.0`,
  - `Microsoft.Extensions.Options.ConfigurationExtensions` from `3.1.0` to `8.0.0`,
  - `Microsoft.Extensions.Primitives` from `3.1.0` to `8.0.0`,
  - `OpenTelemetry.Instrumentation.AspNetCore` from `1.6.0-beta.3` to `1.7.0`,
  - `OpenTelemetry.Instrumentation.EntityFrameworkCore` from `1.0.0-beta.8` to `1.0.0-beta.9`.
- .NET Framework only, following packages updated
  - `Google.Protobuf` updated from `3.25.1` to `3.25.2`,
  - `Grpc.Core.Api` from `2.59.0` to `2.60.0`,
  - `Microsoft.Extensions.Configuration.Binder` from `8.0.0` to `8.0.1`,
  - `Microsoft.Extensions.Options` from `8.0.0` to `8.0.1`,
  - `OpenTelemetry.Instrumentation.AspNet` from `1.6.0-beta.2` to `1.7.0-beta.1`,
  - `System.Text.Json` from `8.0.0` to `8.0.1`.

### Removed

Removed support for `Microsoft.Extensions.Logging`
for `ILOGGER` for versions older than `8.0.0`.

### Fixed

- Set `service.name` resource attribute before invoking the plugin.

## [1.2.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.2.0)

- [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.6.0`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.6.0)
- `System.Diagnostics.DiagnosticSource`: [`8.0.0`](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/8.0.0)

### Added

- Add support for .NET 8.
- Added support for [System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/)
  (NuGet package) traces instrumentation from `4.8.5`.
- Ability to update installation via PS module (`OpenTelemetry.DotNet.Auto.psm1`).

### Changed

#### Dependency updates

- .NET Framework only, `Grpc.Core.Api` updated from `2.57.0` to `2.59.0`.
- .NET only, `OpenTelemetry.Instrumentation.EntityFrameworkCore` updated
  from `1.0.0-beta.7` to `1.0.0-beta.8`.
- .NET only, `OpenTelemetry.Instrumentation.AspNetCore` updated
  from `1.5.1-beta.1` to `1.6.0-beta.3`.
- `OpenTelemetry.Instrumentation.GrpcNetClient`,
  and `OpenTelemetry.Instrumentation.Http`
  updated from `1.5.1-beta.1` to `1.6.0-beta.3`.
- `OpenTelemetry.Instrumentation.SqlClient` updated from `1.5.1-beta.1` to `1.6.0-beta.2`.
- .NET only, `OpenTelemetry.Instrumentation.StackExchangeRedis` updated
  from `1.0.0-rc9.10` to `1.0.0-rc9.12`.
- .NET Framework only, `Google.Protobuf` updated from `3.24.4` to `3.25.1`.
- .NET Framework only, `OpenTelemetry.Instrumentation.AspNet` and
  `OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule` updated from
  `1.6.0-beta.1` to `1.6.0-beta.2`.
- `OpenTelemetry.Instrumentation.Wcf` updated from `1.0.0-rc.12` to `1.0.0-rc.13`.
- .NET Framework only, following packages updated
  - `Microsoft.Bcl.AsyncInterfaces` from `7.0.0` to `8.0.0`,
  - `Microsoft.Extensions.Configuration` from `7.0.0` to `8.0.0`,
  - `Microsoft.Extensions.Configuration.Abstractions` from `7.0.0` to `8.0.0`,
  - `Microsoft.Extensions.Configuration.Binder` from `7.0.4` to `8.0.0`,
  - `Microsoft.Extensions.DependencyInjection` from `7.0.0` to `8.0.0`,
  - `Microsoft.Extensions.DependencyInjection.Abstractions` from `7.0.0` to `8.0.0`,
  - `Microsoft.Extensions.Logging` from `7.0.0` to `8.0.0`,
  - `Microsoft.Extensions.Options` from `7.0.1` to `8.0.0`,
  - `Microsoft.Extensions.Options.ConfigurationExtensions` from `7.0.0` to `8.0.0`,
  - `Microsoft.Extensions.Primitives` from `7.0.0` to `8.0.0`,
  - `System.Text.Encodings.Web` from `7.0.0` to `8.0.0`,
  - `System.Text.Json` from `7.0.3` to `8.0.0`.
- Following packages updated
  - `Microsoft.Extensions.Logging.Abstractions` from `7.0.1` to `8.0.0`,
  - `Microsoft.Extensions.Logging.Configuration` from `7.0.0` to `8.0.0`,
  - `System.Diagnostics.DiagnosticSource` from `7.0.2` to `8.0.0`.

### Removed

- Removed support for `OpenTelemetry.Instrumentation.AspNetCore.AspNetCoreMetricsInstrumentationOptions`
  for plugins.

## [1.1.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.1.0)

### Added

- Added support for `Elastic.Transport` traces instrumentation 0.4.16+.
  `Elastic.Clients.Elasticsearch` 8.10.0+ traces instrumentation is covered by
  `Elastic.Transport` traces instrumentation.
- Added `telemetry.distro.name` resource attribute. The value is set to `opentelemetry-dotnet-instrumentation`.

### Changed

- Change telemetry resource attribute name from `telemetry.auto.version` to `telemetry.distro.version`.

#### Dependency updates

- .NET Framework only, `Google.Protobuf` updated from `3.24.3` to `3.24.4`.
- .NET Framework only, `OpenTelemetry.Instrumentation.AspNet` and
  `OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule` updated from
  `1.0.0-rc9.9` to `1.6.0-beta.1`.

### Fixed

- Fixed log emission issue which resulted in same logs being exported multiple
  times for ASP.NET Core 6.0 apps when bytecode instrumentation was enabled
  and `WebApplicationBuilder` was used.

## [1.0.2](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.0.2)

### Fixed

- Fixed log emission issue for ASP.NET Core 6.0 apps and enhanced diagnostics.

## [1.0.1](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.0.1)

### Changed

#### Dependency updates

- .NET Framework only, `Google.Protobuf` updated from `3.24.2` to `3.24.3`.
- `OpenTelemetry.ResourceDetectors.Azure` updated from `1.0.0-beta.2` to `1.0.0-beta.3`.

### Fixed

- Fixed Rule checking System.Diagnostics.DiagnosticSource version for net7.0
  failing on correct configuration [#2950](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/2950).

## [1.0.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.0.0)

This release is built on top of [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet):

- [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.6.0`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.6.0)
- `System.Diagnostics.DiagnosticSource`: [`7.0.2`](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/7.0.2)

### Added

- Added support for Azure SDK traces instrumentation on .NET Framework.
- Added support for `WCFCLIENT` instrumentation on .NET.

### Changed

- Updated [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.6.0`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.6.0).
- [MySql.Data](https://www.nuget.org/packages/MySql.Data/) instrumentation is now
  supported from version 8.1.0 working on .NET.
- OpenTracing spans are registered under `opentracing-shim` name
  instead of `OpenTelemetry.AutoInstrumentation.OpenTracingShim`.

### Removed

- Removed [MySql.Data](https://www.nuget.org/packages/MySql.Data/) instrumentation
  for versions 6.10.7-8.0.33.
- Removed support for `OpenTelemetry.Instrumentation.MySqlData.MySqlDataInstrumentationOptions`
  for plugins.

### Fixed

- Fixed instrumentation loading issue where delayed instrumentation initialization
  could not bootstrap both traces and metrics.
  Affected scope: ASP.NET Core and HttpClient instrumentations.
- Fixed `ILogger` log instrumentation issue that caused logs to be exported
  multiple times.

## [1.0.0-rc.2](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.0.0-rc.2)

### Added

- Support for Azure App Service resource detector.

- Added `BeforeConfigureTracerProvider`, `BeforeConfigureMeterProvider`,
  `TracerProviderInitialized` and `MeterProviderInitialized` for plugins.
  See [plugins documentation](/docs/plugins.md) for details.
- Added support for Azure SDK traces instrumentation on .NET.

### Changed

- In plugins `ConfigureTracerProvider` and `ConfigureMeterProvider` are changed now
  to `AfterConfigureTracerProvider` and `AfterConfigureMeterProvider`.
  See [plugins documentation](/docs/plugins.md) for details.
- Minimal version of `Grpc.Net.Client` supported on .NET updated to `2.52.0`.

### Fixed

- `OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES` works correctly when .NET CLR Profiler
  is not enabled.
- Fixed manual tracing when instrumented project is referencing
  `System.Diagnostics.DiagnosticSource` `7.0.2`
  [#2780](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/2780).

## [1.0.0-rc.1](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v1.0.0-rc.1)

This release is built on top of [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet):

- [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.5.1`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.5.1)
- `System.Diagnostics.DiagnosticSource`: [`7.0.0`](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/7.0.0)

### Added

- The environment variable `OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED` could be
  used to enable or disable the failing process when
  automatic instrumentation cannot be executed.
- Add support for MySqlConnector traces instrumentation.

### Changed

- Updated [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.5.1`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.5.1).
- ASP.NET instrumentation no longer requires manual modification
  of config files to include `TelemetryHttpModule`.
- Parameter for `ConfigureTracesOptions` extension point for StackExchangeRedis
  changed type from `OpenTelemetry.Instrumentation.StackExchangeRedis.StackExchangeRedisCallsInstrumentationOptions`
  to `OpenTelemetry.Instrumentation.StackExchangeRedis.StackExchangeRedisInstrumentationOptions`.
- `WCF` instrumentation split to `WCFCLIENT` and `WCFSERVICE`.
  Both supported only on .NET Framework.
- `WCFCLIENT` and `WCFSERVICE` no longer requires manual modification
  of config files to include `TelemetryEndpointBehaviorExtensionElement`.
- [GraphQL](https://www.nuget.org/packages/GraphQL/) instrumentation is now
  supported from version 7.5.0 working on .NET.

### Removed

- Removed `WCF` instrumentation for Core WCF Client working on .NET.
- Removed [GraphQL](https://www.nuget.org/packages/GraphQL/) instrumentation
  for versions 2.3.0-2.4.*.

### Known issues

- Lack of support for MySql.Data 8.0.33. See [#2542](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/2542).

## [0.7.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v0.7.0)

### Added

- Stability status added to the documentation.
- Support `OTEL_LOG_LEVEL` to configure SDK logging level.
- Fallback for the service name.
  If the service name is not configured, the automatic instrumentation uses
  the entry assembly name instead, only falling back to the process name
  in case of an error. If the application uses .NET Framework and is hosted
  on IIS, the service name is determined using  `SiteName/ApplicationVirtualPath`.
- Add MongoDB instrumentation support for .NET Framework.
- Added a rule engine to validate potential conflicts and unsupported scenarios,
  ensuring back off instead of crashing, improving overall stability.
- The environment variable `OTEL_DOTNET_AUTO_RULE_ENGINE_ENABLED` could be
  used to enable or disable the rule engine.
- Support for Container resource detector.
- Support for enabling well known resource detectors
  by using the environment variables
  - `OTEL_DOTNET_AUTO_RESOURCE_DETECTOR_ENABLED`
  - `OTEL_DOTNET_AUTO_{0}_RESOURCE_DETECTOR_ENABLED`.

### Removed

- Remove support for enabling debugging mode with `OTEL_DOTNET_AUTO_DEBUG`.
- Removed `OTEL_DOTNET_AUTO_INTEGRATIONS_FILE` as a required environment
  variable for bytecode instrumentation setup

## [0.6.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v0.6.0)

This release is built on top of [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet):

- [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.4.0`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.4.0)
- `System.Diagnostics.DiagnosticSource`: [`7.0.0`](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/7.0.0)

### Changed

- Updated [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.4.0`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.4.0).

## [0.6.0-beta.2](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v0.6.0-beta.2)

This beta release is built on top of [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet):

- [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.4.0-rc.4`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.4.0-rc.4)
- `System.Diagnostics.DiagnosticSource`: [`7.0.0`](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/7.0.0)

### Added

- Support for systems with glibc versions 2.17-2.29.

### Changed

- Updated [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.4.0-rc.4`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.4.0-rc.4).
- Replace `OTEL_DOTNET_AUTO_LEGACY_SOURCES` with `OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_LEGACY_SOURCES`.
- Updated the shared store to correctly support
  [framework roll-forward](https://learn.microsoft.com/en-us/dotnet/core/versions/selection#framework-dependent-apps-roll-forward)
  from `net6.0` to `net7.0`.

### Removed

- Remove support for plugin method `ConfigureMetricsOptions(OpenTelemetry.Instrumentation.Process.ProcessInstrumentationOptions)`.

### Fixed

- Fix location of `OpenTelemetry.AutoInstrumentation.Native.so` for `linux-musl-x64`.
- Fix issues when instrumenting `dotnet` CLI
  [#1477](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/1744).

## [0.6.0-beta.1](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v0.6.0-beta.1)

This beta release is built on top of [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet):

- [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.4.0-rc.3`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.4.0-rc.3)
- `System.Diagnostics.DiagnosticSource`: [`7.0.0`](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/7.0.0)

### Added

- Support configuring `OTEL_*` settings using `App.config` and `Web.config`.
- Add support for Quartz traces instrumentation.
- Add support for EntityFrameworkCore traces instrumentations.
- Add plugin support for
  `ResourceBuilder ConfigureResource(ResourceBuilder builder)`.

### Changed

- Updated [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.4.0-rc.3`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.4.0-rc.3)
- Move `OpenTelemetry.AutoInstrumentation.Native.so` to `linux-x64` directory
  in `tracer-home` for Linux glibc, `OpenTelemetry.AutoInstrumentation.Native.so`
  to `linux-musl-x64` for Linux musl and
  `OpenTelemetry.AutoInstrumentation.Native.dylib`
  to `osx-x64` for MacOS.
- Change the way to manage enabled instrumentations. The following environmental
  variables:
  - `OTEL_DOTNET_AUTO_TRACES_ENABLED_INSTRUMENTATIONS`,
  - `OTEL_DOTNET_AUTO_TRACES_DISABLED_INSTRUMENTATIONS`,
  - `OTEL_DOTNET_AUTO_METRICS_ENABLED_INSTRUMENTATIONS`,
  - `OTEL_DOTNET_AUTO_METRICS_DISABLED_INSTRUMENTATIONS`,
  - `OTEL_DOTNET_AUTO_LOGS_ENABLED_INSTRUMENTATIONS`,
  - `OTEL_DOTNET_AUTO_LOGS_DISABLED_INSTRUMENTATIONS`

  are replaced by:

  - `OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED`,
  - `OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED`,
  - `OTEL_DOTNET_AUTO_TRACES_{0}_INSTRUMENTATION_ENABLED`,
  - `OTEL_DOTNET_AUTO_METRICS_INSTRUMENTATION_ENABLED`,
  - `OTEL_DOTNET_AUTO_METRICS_{0}_INSTRUMENTATION_ENABLED`,
  - `OTEL_DOTNET_AUTO_LOGS_INSTRUMENTATION_ENABLED`,
  - `OTEL_DOTNET_AUTO_LOGS_{0}_INSTRUMENTATION_ENABLED`.

- Change instrumentation id for ASP.NET Core traces and metrics instrumentation
  from `AspNet` to `ASPNETCORE`.

### Fixed

- Fix console error messages `Log: Exception creating FileSink`
 [#1885](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/1885)

## [0.5.1-beta.3](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v0.5.1-beta.3)

This beta release is built on top of [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet):

- [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.4.0-rc.1`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.4.0-rc.1)
- `System.Diagnostics.DiagnosticSource`: [`7.0.0`](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/7.0.0)

### Added

- Add support for NServiceBus metrics and traces instrumentations.
- Add support for Elasticsearch traces instrumentations.
- Add plugin support for
 `ConfigureTracesOptions(StackExchangeRedisCallsInstrumentationOptions options)`.
- Add plugin support for
 `ConfigureMetricsOptions(AspNetCoreMetricsInstrumentationOptions options)`.
- Add automatic assembly redirection for .NET Framework applications. The redirection
 can be enabled or disabled via the
 `OTEL_DOTNET_AUTO_NETFX_REDIRECT_ENABLED` environment variable.
 See the [additional settings](./docs/config.md#additional-settings) table for details.
- Add automatic Global Assembly Cache (GAC) registration, of the distributed
 .NET Framework assemblies, to the PowerShell installation module.

### Changed

- Updated [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.4.0-rc.1`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.4.0-rc.1)

### Removed

- Remove support for Jaeger exporter.

### Fixed

- Fix WCF instrumentation on .NET Framework.

## [0.5.1-beta.2](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v0.5.1-beta.2)

### Added

- Add support for `OTEL_TRACES_SAMPLER` and `OTEL_TRACES_SAMPLER_ARG`.
- Add `Initializing` plugin extension point
  that is invoked before OpenTelemetry SDK configuration.

## [0.5.1-beta.1](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v0.5.1-beta.1)

This beta release is built on top of [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet):

- [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.4.0-beta.3`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.4.0-beta.3)
- `System.Diagnostics.DiagnosticSource`: [`7.0.0`](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/7.0.0)

### Added

- Add support for `OTEL_BSP_SCHEDULE_DELAY`, `OTEL_BSP_EXPORT_TIMEOUT`,
  `OTEL_BSP_MAX_QUEUE_SIZE`, `OTEL_BSP_MAX_EXPORT_BATCH_SIZE`.
- Add support for `OTEL_METRIC_EXPORT_TIMEOUT`.
- Add support for `OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT`, `OTEL_ATTRIBUTE_COUNT_LIMIT`,
  `OTEL_SPAN_ATTRIBUTE_VALUE_LENGTH_LIMIT`, `OTEL_SPAN_ATTRIBUTE_COUNT_LIMIT`,
  `OTEL_SPAN_EVENT_COUNT_LIMIT`, `OTEL_SPAN_LINK_COUNT_LIMIT`,
  `OTEL_EVENT_ATTRIBUTE_COUNT_LIMIT`, `OTEL_LINK_ATTRIBUTE_COUNT_LIMIT`
  for `otlp` exporter.

### Changed

- Updated [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.4.0-beta.3`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.4.0-beta.3)
- Updated plugins method signature to overwrite OpenTelemetry .NET SDK exporters'
  and instrumentations' options. `ConfigureOptions` changed to `ConfigureTracesOptions`,
  `ConfigureMetricsOptions` or `ConfigureLogsOptions`.

## [0.5.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v0.5.0)

The is the first production-ready (non-beta) release.
It is not stable yet.

### Added

- Add support for .NET 7.
- Add support for `OTEL_DOTNET_AUTO_LOGS_ENABLED`.
- Add error log if bytecode instrumentation type
  is missing all instrumentation methods.
- Plugins can overwrite OpenTelemetry .NET SDK exporters' and instrumentations' options.

### Changed

- Replace `OTEL_DOTNET_AUTO_LOAD_TRACER_AT_STARTUP` with `OTEL_DOTNET_AUTO_TRACES_ENABLED`
  and `OTEL_DOTNET_AUTO_LOAD_METER_AT_STARTUP` with `OTEL_DOTNET_AUTO_METRICS_ENABLED`.
- Disable OpenTracing by default. OpenTracing can be re-enabled via `OTEL_DOTNET_AUTO_OPENTRACING_ENABLED`.
- GraphQL exceptions are recorded as OTel events.
- `DOTNET_STARTUP_HOOKS` required value changed to `$INSTALL_DIR/net/OpenTelemetry.AutoInstrumentation.StartupHook.dll`.

### Removed

- Remove support for .NET Core 3.1.
- Remove support for `OTEL_DOTNET_AUTO_HTTP2UNENCRYPTEDSUPPORT_ENABLED`.
- Remove support for `OTEL_DOTNET_AUTO_ENABLED`.
  Use `CORECLR_ENABLE_PROFILING` or `COR_ENABLE_PROFILING` instead.
- Remove support for `OTEL_DOTNET_AUTO_INCLUDE_PROCESSES`.

### Fixed

- Fix the IIS registration in the PowerShell script module for Windows Server 2016.
- Fix the IIS unregistration in the PowerShell script module.
- Get rid of unnecessary service restarts during the IIS unregistration,
  in the PowerShell script module.
- `OTEL_DOTNET_AUTO_TRACES_ENABLED` is also respected by bytecode instrumentations.

## [0.4.0-beta.1](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v0.4.0-beta.1)

### Added

- Add WCF traces instrumentation (server-side for .NET Framework, client-side
  for both .NET Core and .NET Framework).
- Support ASP.NET Core OpenTelemetry Log exporter related environment variables:
  - `OTEL_LOGS_EXPORTER`,
  - `OTEL_DOTNET_AUTO_LOGS_CONSOLE_EXPORTER_ENABLED`,
  - `OTEL_DOTNET_AUTO_LOGS_ENABLED_INSTRUMENTATIONS`,
  - `OTEL_DOTNET_AUTO_LOGS_DISABLED_INSTRUMENTATIONS`,
  - `OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE`.
- Support `OTEL_DOTNET_AUTO_GRAPHQL_SET_DOCUMENT` (default value: `false`)
  environment variable which controls whether `graphql.document` attribute
  is set.
- Add ILogger logging instrumentation for .NET Core 3.1+.  
- Add [telemetry resource attributes](https://github.com/open-telemetry/opentelemetry-specification/tree/v1.13.0/specification/resource/semantic_conventions#telemetry-sdk).
- Add support for the `b3` propagator.
- Add MassTransit traces instrumentation.
- Add `OpenTelemetry.AutoInstrumentation` Nuget package.
- Support for Process metrics collection using
  the `OpenTelemetry.Instrumentation.Process` package.
- Add Shell scripts for downloading and installing OpenTelemetry .NET Automatic Instrumentation
  and instrumenting .NET applications.
- Add PowerShell script module for downloading and installing
  OpenTelemetry .NET Automatic Instrumentation
  and instrumenting .NET applications.

### Changed

- Replaced `OTEL_DOTNET_AUTO_TRACES_PLUGINS` and `OTEL_DOTNET_AUTO_METRICS_PLUGINS`
  with new environment variable `OTEL_DOTNET_AUTO_PLUGINS`.
- Adjusted tags for MongoDB integration. See [pull request](https://github.com/jbogard/MongoDB.Driver.Core.Extensions.DiagnosticSources/pull/18)
  for more details.
- Extend MySql.Data traces instrumentation for versions 8.0.31+.
  Versions 8.0.31+ require bytecode instrumentation.

### Removed

- Removed support for MongoDB integration for [MongoDB.Driver.Core](https://www.nuget.org/packages/MongoDB.Driver.Core)
  prior to 2.13.3.

### Fixed

- Log folder structure is fully created on Linux.
- Update GraphQL instrumentation to follow the [OpenTelemetry semantic conventions](https://github.com/open-telemetry/opentelemetry-specification/blob/v1.13.0/specification/trace/semantic_conventions/instrumentation/graphql.md).
- Fixed the race between requesting ReJIT of methods targeted for bytecode
 instrumentation and their first execution. The race allowed, in rare occasions,
 for the first few executions of the method to not be instrumented. See
 issue [#1242](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/1242).
- Span kind for GraphQL instrumentation is set as span property instead of attribute.
- Application crash if "wrapper type" from bytecode instrumentation is missing
 [#1469](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/1469).

## [0.3.1-beta.1](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v0.3.1-beta.1)

This release is built on top of [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet):

- [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.3.1`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.3.1)
- `System.Diagnostics.DiagnosticSource`: [`6.0.0`](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/6.0.0)

### Added

- Add support for Alpine.
- Add strong name signature to the OpenTelemetry.AutoInstrumentation assembly used
  on the .NET Framework.

### Changed

- Extend StackExchange.Redis traces instrumentation for versions 2.6.66+.
- Updated [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.3.1`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.3.1)

## [0.3.0-beta.1](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v0.3.0-beta.1)

This release add various new instrumentations and more propagation options.

### Added

- Add Grpc.Net.Client traces instrumentation.
- Add MySql.Data traces instrumentation.
- Add Npgsql traces instrumentation.
- Add StackExchange.Redis traces instrumentation.
- Add configuration option `none` to `OTEL_DOTNET_AUTO_TRACES_ENABLED_INSTRUMENTATIONS`
  and `OTEL_DOTNET_AUTO_METRICS_ENABLED_INSTRUMENTATIONS`.
- Add support for the `b3multi` propagator.
- Add support for the `OTEL_PROPAGATORS` environment variable.
  Supported configuration options are `b3multi`, `baggage`, `tracecontext`.
  Default is `tracecontext,baggage`.

### Changed

- Renamed `OTEL_DOTNET_AUTO_TRACES_ENABLED` to `OTEL_DOTNET_AUTO_ENABLED` since it
  controls enabling or disabling the CLR profiler independent of the signal type.
- `OTEL_DOTNET_AUTO_TRACES_ENABLED_INSTRUMENTATIONS` default value is changed to
  include all of the available instrumentations.
- `OTEL_DOTNET_AUTO_METRICS_ENABLED_INSTRUMENTATIONS` default value is changed to
  include all of the available instrumentations.
- Changed Tracing sampler from `always_on` to `parentbased_always_on`.
  See [the OpenTelemetry specification](https://github.com/open-telemetry/opentelemetry-specification/blob/6ce62202e5407518e19c56c445c13682ef51a51d/specification/sdk-environment-variables.md?plain=1#L46)
  for more details.

## Removed

- Remove `OTEL_DOTNET_AUTO_DOMAIN_NEUTRAL_INSTRUMENTATION` configuration
  as it is not needed.
- Remove `OTEL_DOTNET_AUTO_{0}_ENABLED` configuration,
  use `OTEL_DOTNET_AUTO_[TRACES/METRICS]_[ENABLED/DISABLED]_INSTRUMENTATIONS`
  instead.
- Remove `OTEL_DOTNET_AUTO_METRICS_ENABLED` configuration as it is not needed.

## [0.2.0-beta.1](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v0.2.0-beta.1)

The main feature of this release is the support for the metrics signal.

This release is built on top of [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet):

- [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.3.0`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.3.0)
- `System.Diagnostics.DiagnosticSource`: [`6.0.0`](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/6.0.0)

You can find all OpenTelemetry references in
[OpenTelemetry.AutoInstrumentation.csproj](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v0.2.0-beta.1/src/OpenTelemetry.AutoInstrumentation/OpenTelemetry.AutoInstrumentation.csproj).

### Added

- Add MongoDB instrumentation support from .NET Core 3.1+.
- Support for OpenTelemetry metric exporter related environment variables:
  - `OTEL_DOTNET_AUTO_METRICS_ENABLED`,
  - `OTEL_DOTNET_AUTO_LOAD_METER_AT_STARTUP`,
  - `OTEL_METRICS_EXPORTER`,
  - `OTEL_DOTNET_AUTO_METRICS_CONSOLE_EXPORTER_ENABLED`,
  - `OTEL_DOTNET_AUTO_METRICS_ENABLED_INSTRUMENTATIONS`,
  - `OTEL_DOTNET_AUTO_METRICS_DISABLED_INSTRUMENTATIONS`,
  - `OTEL_DOTNET_AUTO_METRICS_PLUGINS`,
  - `OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES`.
- Support for .NET Runtime metrics collection using
  the `OpenTelemetry.Instrumentation.Runtime` package.
- Support for ASP.NET and HttpClient metrics instrumentations.
- Support for Prometheus Exporter HttpListener version.
- `OTEL_DOTNET_AUTO_INTEGRATIONS_FILE` can accept multiple filepaths
  delimited by the platform-specific path separator
  (`;` on Windows, `:` on Linux and macOS).
- Support for metric exporter interval using environment variable:
  `OTEL_METRIC_EXPORT_INTERVAL`.

### Changed

- Rename generic environment variables to include trace.
  - `OTEL_DOTNET_AUTO_ENABLED` &#8594; `OTEL_DOTNET_AUTO_TRACES_ENABLED`,
  - `OTEL_DOTNET_AUTO_LOAD_AT_STARTUP` &#8594; `OTEL_DOTNET_AUTO_LOAD_TRACER_AT_STARTUP`,
  - `OTEL_DOTNET_AUTO_CONSOLE_EXPORTER_ENABLED` &#8594; `OTEL_DOTNET_AUTO_TRACES_CONSOLE_EXPORTER_ENABLED`,
  - `OTEL_DOTNET_AUTO_ENABLED_INSTRUMENTATIONS` &#8594; `OTEL_DOTNET_AUTO_TRACES_ENABLED_INSTRUMENTATIONS`,
  - `OTEL_DOTNET_AUTO_DISABLED_INSTRUMENTATIONS` &#8594; `OTEL_DOTNET_AUTO_TRACES_DISABLED_INSTRUMENTATIONS`,
  - `OTEL_DOTNET_AUTO_INSTRUMENTATION_PLUGINS` &#8594; `OTEL_DOTNET_AUTO_TRACES_PLUGINS`,
  - `OTEL_DOTNET_AUTO_ADDITIONAL_SOURCES` &#8594; `OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES`.
  
### Removed

- Support for .NET 5.0

### Fixed

- Use `,` as separator, as documented, instead of `;`, for:
  - `OTEL_DOTNET_AUTO_INCLUDE_PROCESSES`,
  - `OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES`,
  - `OTEL_DOTNET_AUTO_TRACES_DISABLED_INSTRUMENTATIONS`.
- Remove invalid instrumentation for `MongoDB.Driver.Core` <2.3.0.

## [0.1.0-beta.1](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/tag/v0.1.0-beta.1)

The is an initial, official beta release,
built on top of [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet):

- [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
  [`1.2.0`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.2.0)
- Non-core components: [`1.0.0-rc9.2`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/1.0.0-rc9.2)
- `System.Diagnostics.DiagnosticSource`: [`6.0.0`](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/6.0.0)

### Added

- Support for .NET Framework 4.6.2 and higher.
- Support for .NET Core 3.1.
- Support for .NET 5.0 and 6.0.
- ASP.NET and ASP.NET Core source instrumentations.
- [GraphQL](https://www.nuget.org/packages/GraphQL/) bytecode instrumentation.
- [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient)
  and [System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient)
  source instrumentation.
- OTLP, Jaeger, Zipkin and Console trace exporters.
- Global management using environment variables:
  `OTEL_DOTNET_AUTO_HOME`, `OTEL_DOTNET_AUTO_ENABLED`,
  `OTEL_DOTNET_AUTO_INCLUDE_PROCESSES`, `OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES`,
  `OTEL_DOTNET_AUTO_AZURE_APP_SERVICES`.
- Support for OpenTelemetry resource environment variables:
  `OTEL_RESOURCE_ATTRIBUTES`, `OTEL_SERVICE_NAME`.
- Instrumentation management using environment variables:
  `OTEL_DOTNET_AUTO_INTEGRATIONS_FILE`, `OTEL_DOTNET_AUTO_ENABLED_INSTRUMENTATIONS`,
  `OTEL_DOTNET_AUTO_DISABLED_INSTRUMENTATIONS`,
  `OTEL_DOTNET_AUTO_{0}_ENABLED`,
  `OTEL_DOTNET_AUTO_DOMAIN_NEUTRAL_INSTRUMENTATION`,
  `OTEL_DOTNET_AUTO_CLR_DISABLE_OPTIMIZATIONS`,
  `OTEL_DOTNET_AUTO_CLR_ENABLE_INLINING`,
  `OTEL_DOTNET_AUTO_CLR_ENABLE_NGEN`.
- Support for OpenTelemetry exporter related environment variables:
  `OTEL_TRACES_EXPORTER`,
  `OTEL_EXPORTER_OTLP_PROTOCOL`,
- Customization and plugin capabilities which can be configured
  using the following environment variables:
  `OTEL_DOTNET_AUTO_LOAD_AT_STARTUP`,
  `OTEL_DOTNET_AUTO_ADDITIONAL_SOURCES`,
  `OTEL_DOTNET_AUTO_LEGACY_SOURCES`,
  `OTEL_DOTNET_AUTO_INSTRUMENTATION_PLUGINS`.
- `OTEL_DOTNET_AUTO_HTTP2UNENCRYPTEDSUPPORT_ENABLED` environment variable
  which enables `System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport`.
  See the [official Microsoft documentation](https://docs.microsoft.com/en-us/aspnet/core/grpc/troubleshoot?view=aspnetcore-6.0#call-insecure-grpc-services-with-net-core-client)
  for more details.
