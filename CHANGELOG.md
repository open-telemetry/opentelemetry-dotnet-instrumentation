# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
This project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/compare/v0.2.0-beta.1...HEAD)

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

- Adds MongoDB instrumentation support from .NET Core 3.1+.
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
