# dotnet-monitor OTLP Egress

This document covers the ongoing effort to support OpenTelemetry native OTLP
egress (export) from dotnet-monitor.

## Goal

Users should be able to listen to a .NET process externally (out of process)
using dotnet-monitor and send off OTLP data.

## Design overview

![Out-of-proc Instrumentation](https://github.com/user-attachments/assets/7f0b2870-f95a-4d73-9c6e-101a58d3e828)

Event sources:

* Tracing:
  <https://github.com/dotnet/runtime/blob/main/src/libraries/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/DiagnosticSourceEventSource.cs>

* Metrics:
  <https://github.com/dotnet/runtime/blob/main/src/libraries/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/Metrics/MetricsEventSource.cs>

* Logging:
  <https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Logging.EventSource/src/LoggingEventSource.cs>

There are three different repos in play in the design:

* <https://github.com/dotnet/diagnostics/>

  Contains the low-level functionality used by various dotnet diagnostic tools
  (dotnet-monitor, dotnet-counters, dotnet-trace, etc.). The clients used to
  perform diagnostics as well as the event pipe logic to read from
  `EventSource`s.

* <https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation>

  Is going to contain the mini-OpenTelemetry SDK dotnet-monitor will use to do
  the heavy lifting. This is being called a mini-SDK because it only needs to
  support the processor/exporter portions of the OpenTelemetry SDK. Logging is
  implemented as Log Bridge. Metrics uses MetricProducer. Tracing doesn't really
  have a forwarding concept, so there is just an API to submitted an "ended"
  span.

* <https://github.com/dotnet/dotnet-monitor>

  Contains the code for dotnet-monitor. The goal for the dotnet-monitor team is
  to own as little OpenTelemetry code as possible. It should all be loaded from
  external package(s). What dotnet-monitor will do is load the OpenTelemetry
  mini-SDK, give it an `IConfigurationSection` from its settings file to map
  onto options, and feed it data read from the target process.

## Supported scenarios

The out-of-proc model doesn't use any bytecode manipulation whatsoever. Users
are able to listen to telemetry (created via ActivitySource, Meter, and/or
ILogger) emitted by runtime, their own code, or in any libraries they use. That
instrumentation needs to be present when the application is deployed.

### Native runtime instrumentation

* HttpClient as of .NET 9 supports tracing & metrics.

* AspNetCore as of .NET 9 supports metrics. Limited tracing (spans are created
  but not populated).

* Runtime metrics are available as of .NET 9.

### Propagation

There is no ability currently for dotnet-monitor to configure propagation.
Propagation in the target process will be owned by
[DistributedContextPropagator](https://learn.microsoft.com/dotnet/api/system.diagnostics.distributedcontextpropagator).
Default style is W3C.

### Trace sampling

There is limited ability for dotnet-monitor to configure sampling. When
listening to the tracing EventSource subscribers may pass a string which
controls what the EventSource `ActivityListener` will do.
[Sampling](https://github.com/dotnet/runtime/blob/87e9f1d94f94f7e9b38da74fd93ea856b0ca6d92/src/libraries/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/DiagnosticSourceEventSource.cs#L60)
style is an option there. dotnet-monitor may configure the sampler but it can't
bring custom samplers, it can only choose from one of the samplers available in
runtime.

### Metric aggregation and cardinality limits

When there is a listener to the metrics EventSource runtime has its own sort of
mini-OTel SDK which does aggregation. Aggregated data is periodically written to
the EventSource. dotnet-monitor can configure the [cardinality
limit](https://github.com/dotnet/runtime/blob/87e9f1d94f94f7e9b38da74fd93ea856b0ca6d92/src/libraries/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/Metrics/MetricsEventSource.cs#L37)
(more or less).

### Log filtering

The logging EventSource supports the same type of ILogger configuration which
may be done via `IConfiguration` in the target app. dotnet-monitor can listen to
[different categories (with prefix support) and log
levels](https://github.com/dotnet/runtime/blob/87e9f1d94f94f7e9b38da74fd93ea856b0ca6d92/src/libraries/Microsoft.Extensions.Logging.EventSource/src/LoggingEventSource.cs#L36).

## Proof-of-concept

A proof of concept was built.

* <https://github.com/CodeBlanch/diagnostics/tree/traces-pipeline>

  Contains the changes to the dotnet-diagnostics repo. Adds more data in the
  metrics pipeline to support more of the OTLP data model. Adds a new logging
  pipeline. The existing logging pipeline routes into ILogger which has some
  issues. The new logging pipeline spits out LogRecords directly. Adds a
  distributed tracing (Activity) pipeline. Distributed tracing is not currently
  supported anywhere in these tools.

* <https://github.com/CodeBlanch/dotnet-monitor/tree/otel-poc>

  Contains the dotnet-monitor code and the mini-SDK.

  The mini-SDK is mostly:
  <https://github.com/CodeBlanch/dotnet-monitor/tree/otel-poc/src/Microsoft.Diagnostics.Monitoring.OpenTelemetry>

  But there are also options here:
  <https://github.com/CodeBlanch/dotnet-monitor/tree/otel-poc/src/Tools/dotnet-monitor/OpenTelemetry/Options>

  The dotnet-monitor team doesn't want to own options. They just want to pass
  `IConfiguration` to the mini-SDK so those options should be moved.

  The main dotnet-monitor code which establishes the pipelines and sets up the
  SDK is here:
  <https://github.com/CodeBlanch/dotnet-monitor/blob/otel-poc/src/Tools/dotnet-monitor/OpenTelemetry/OpenTelemetryService.cs>

  There is some support for `Resource` configuration in the PoC. Users may set
  static resource key/values or they can set keys which will be
  populated/retrieved from the target process environment variable set.

  **Challenge** That `OpenTelemetryService.cs` file _may_ contain more logic
  than what the dotnet-monitor team will accept. In theory more could be moved
  into the mini-SDK owned by Auto-Instrumentation but dotnet/diagnostics does
  not ship a proper public API. This code uses a lot of
  [InternalsVisibleTo](https://github.com/dotnet/diagnostics/blob/147534f6a07410bb618eebf12b96a58566bb3c5d/src/Microsoft.Diagnostics.Monitoring.EventPipe/Microsoft.Diagnostics.Monitoring.EventPipe.csproj#L41-L48)
  ~~hacks~~ features to compile. More code could be handled by the mini-SDK if
  it was able to see some of these types.

## Completed work and open PRs

### .NET 9

* <https://github.com/dotnet/runtime/pull/107576>

  We added a version to the EventSources so dotnet-monitor can determine which
  features are available. For example ParentBased(Ratio) sampling only works on
  .NET9+.

* <https://github.com/dotnet/runtime/pull/105581>

  dotnet-monitor today can listen to metrics but you have to add everything
  explicitly. A wildcard feature was added so users can easily listen to things
  like "MyCompany.*".

* <https://github.com/dotnet/runtime/pull/104134>

  Before this change if a listener subscribes to the tracing EventSource it is
  basically in "AlwaysOn" mode meaning every Activity is created. We added a
  sampler mode which is more or less "ParentBased(TraceIdRatioBased)" to give
  users more control.

* <https://github.com/dotnet/runtime/pull/103655>

  Before this change log correlation didn't work.

### Post-PoC

Work to move the dotnet/diagnostics changes from the PoC into the actual code:

* <https://github.com/dotnet/diagnostics/pull/5124>
* <https://github.com/dotnet/diagnostics/pull/5120>
* <https://github.com/dotnet/diagnostics/pull/5078>

### Known gaps

* <https://github.com/dotnet/runtime/issues/109388>

  The current metrics EventSource doesn't expose a proper histogram. It only
  exposes what would be called a Summary histogram in OTel world.

* <https://github.com/dotnet/runtime/issues/102924>

  There is no way to listen to Activity Links or Events using the existing
  tracing EventSource.

* <https://github.com/dotnet/aspnetcore/issues/52439>

  AspNetCore doesn't populate spans with data.
