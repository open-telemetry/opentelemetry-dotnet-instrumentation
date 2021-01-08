# opentelemetry-dotnet-instrumentation
[![Gitter
chat](https://badges.gitter.im/open-telemetry/opentelemetry-dotnet.svg)](https://gitter.im/open-telemetry/opentelemetry-dotnet-auto-instr)

This project provides a .NET tracer that leverages the .NET profiling APIs to support .NET instrumentation and auto-instrumentation without requiring code changes to an application.

## Status

This project is in the early stages of development starting with an initial seeding of code from the [.NET Tracer for Datadog APM](https://github.com/DataDog/dd-trace-dotnet). Our current goal is to take the seeded tracer and update it to both listen to and generate OpenTelemetry tracing data. To accomplish this our current priorities are to:
1. Define System.Diagnostics.DiagnosticSource wrappers to generate and consume [.NET Activities](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity?view=net-5.0)
2. Validate that the performance of this wrapping approach will be acceptable.

For more details about the preliminary roadmap refer to the [Preliminary Roadmap Google doc](https://docs.google.com/document/d/10BiAfYDURrk8PQxjT65bEc0ydVngWLoWk8IGo4xDKko/edit?usp=sharing).

## Contributing

We meet weekly on Wednesdays at 1PM PT. The meeting is subject to change depending on contributors'
availability. Check the [OpenTelemetry community
calendar](https://calendar.google.com/calendar/embed?src=google.com_b79e3e90j7bbsa2n2p5an5lf60%40group.calendar.google.com)
for specific dates.

Meetings take place via [Zoom video conference](https://zoom.us/j/8287234601).

Meeting notes are available as a public [Google
doc](https://docs.google.com/document/d/1XedN2D8_PH4YLej-maT8sp4RKogfuhFpccRi3QpUcoI/edit?usp=sharing).
For edit access, get in touch on
[Gitter](https://gitter.im/open-telemetry/opentelemetry-dotnet-auto-instr).

## Community Roles

Maintainers ([@open-telemetry/dotnet-instrumentation-maintainers](https://github.com/orgs/open-telemetry/teams/dotnet-instrumentation-maintainers)):

- [Chris Ventura](https://github.com/nrcventura), New Relic
- [Greg Paperin](https://github.com/macrogreg), Datadog
- [Lucas Pimentel-Ordyna](https://github.com/lucaspimentel), Datadog
- [Paulo Janotti](https://github.com/pjanotti), Splunk

Approvers ([@open-telemetry/dotnet-instrumentation-approvers](https://github.com/orgs/open-telemetry/teams/dotnet-instrumentation-approvers)):

- [Colin Higgins](https://github.com/colin-higgins), Datadog
- [Kevin Gosse](https://github.com/kevingosse), Datadog
- [Mike Goldsmith](https://github.com/MikeGoldsmith), HoneyComb
- [Tony Redondo](https://github.com/tonyredondo), Datadog
- [Zach Montoya](https://github.com/zacharycmontoya), Datadog

Learn more about roles in the [community repository](https://github.com/open-telemetry/community/blob/master/community-membership.md).
