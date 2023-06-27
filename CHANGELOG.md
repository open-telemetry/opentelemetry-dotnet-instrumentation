# Changelog

All notable changes to this component are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
This component adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/compare/v1.0.0-rc.1...HEAD)

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

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
