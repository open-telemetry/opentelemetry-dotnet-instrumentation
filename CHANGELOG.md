# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
This project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

The is an initial, official beta release,
built on top of [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet):

- Core components: [`1.2.0-rc4`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.2.0-rc4)
- Non-core components: [`1.0.0-rc9.1`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/1.0.0-rc9.1)
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
  `OTEL_EXPORTER_JAEGER_AGENT_HOST`, `OTEL_EXPORTER_JAEGER_AGENT_PORT`,
  `OTEL_EXPORTER_JAEGER_ENDPOINT`,
  `OTEL_EXPORTER_JAEGER_PROTOCOL`,
  `OTEL_EXPORTER_OTLP_ENDPOINT`,
  `OTEL_EXPORTER_OTLP_HEADERS`,
  `OTEL_EXPORTER_OTLP_TIMEOUT`,
  `OTEL_EXPORTER_OTLP_PROTOCOL`,
  `OTEL_EXPORTER_ZIPKIN_ENDPOINT`.
- Support for OpenTelemetry batch span processor related environment variables:
  `OTEL_BSP_SCHEDULE_DELAY`,
  `OTEL_BSP_EXPORT_TIMEOUT`,
  `OTEL_BSP_MAX_QUEUE_SIZE`,
  `OTEL_BSP_MAX_EXPORT_BATCH_SIZE`.
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
