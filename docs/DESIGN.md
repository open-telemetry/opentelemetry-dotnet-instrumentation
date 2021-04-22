# Design

## Vision

> High-level goals defining the long term vision for the project that will guide daily activities, its design, and feature acceptance.

- **High performance**: auto-instrumentation performance impact should not be a concern for its users.
- **Reliability**: stable and performant under different loads. Well-behaved under extreme load, with predictable, low resource consumption.
- **Visibility**: users should be able to generate telemetry data that provides deep and detailed visibility into their applications. Such telemetry must allow users to identify and solve application-related issues in production.
- **Useful out-of-the-box**: after install users should be able to get telemetry data from targeted libraries with none or minimal configuration (good selection of defaults).
- **Extensible**: key components can be chosen either at build time.

## Constraints

- No compliance to Common Language Specification (CLS). More: [#131](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/131).

## Plans

[Preliminary Roadmap Google doc](https://docs.google.com/document/d/1F25EzxYa7iSs2r9u0kjetCNPGS7Ui-bneHJEEwzEFR4/edit#heading=h.8ps4qge8rkv6)

### Linkage with spans created via [OpenTelemetry .NET SDK](https://github.com/open-telemetry/opentelemetry-dotnet)

Currnelty, in short term, the spans can properly linked with ones created using [OpenTracing](https://github.com/opentracing/opentracing-csharp).

In future, it is planned to properly link the spans created using [OpenTelemetry .NET SDK](https://github.com/open-telemetry/opentelemetry-dotnet).

Tracked under [#13](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/13).

### Reuse of [OpenTelemetry .NET SDK](https://github.com/open-telemetry/opentelemetry-dotnet)

Currently, in short term, we are reimplementing Open Telemetry components like Exporters, Propagators which are already implemented in [OpenTelemetry .NET SDK](https://github.com/open-telemetry/opentelemetry-dotnet). This is mainly because the current tracing model is different than in [OpenTelemetry .NET SDK](https://github.com/open-telemetry/opentelemetry-dotnet).

In future, it is planned to redesign the model so that we could reuse code implemented in [OpenTelemetry .NET SDK](https://github.com/open-telemetry/opentelemetry-dotnet).

Reference: [#107](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/107).

## Further Reading

OpenTelemetry:
- [OpenTelemetry website](https://opentelemetry.io/)
- [OpenTelemetry Specification](https://github.com/open-telemetry/opentelemetry-specification)

Microsoft .NET Profiling APIs:
- [Profiling API](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/)
- [Metadata API](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/metadata/)
- [The Book of the Runtime - Profiling](https://github.com/dotnet/coreclr/blob/master/Documentation/botr/profiling.md)

OpenTracing:
- [OpenTracing documentation](https://github.com/opentracing/opentracing-csharp)
- [OpenTracing terminology](https://github.com/opentracing/specification/blob/master/specification.md)

Datadog APM (from which this repository originates):
- [Datadog APM - Tracing .NET Core and .NET 5 Applications](https://docs.datadoghq.com/tracing/setup_overview/setup/dotnet-core)
- [Datadog APM - Tracing .NET Framework Applications](https://docs.datadoghq.com/tracing/setup_overview/setup/dotnet-framework)
