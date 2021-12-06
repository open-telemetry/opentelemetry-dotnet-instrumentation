# OpenTelemetry .NET Auto-Instrumentation

[![Slack](https://img.shields.io/badge/slack-@cncf/otel--dotnet--auto--instr-brightgreen.svg?logo=slack)](https://cloud-native.slack.com/archives/C01NR1YLSE7)

This project provides a .NET tracer that leverages the .NET profiling APIs to support .NET instrumentation and auto-instrumentation without requiring code changes to an application.

## Status

This project is in the early stages of development starting with an initial seeding of code from the [.NET Tracer for Datadog APM](https://github.com/DataDog/dd-trace-dotnet). There is a [project board](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/projects/1) showing the current work in progress and the backlog.

For more details about the design and roadmap see [DESIGN.md](DESIGN.md).

## Compatibility

OpenTelemetry .NET Auto-Instrumentation attempts to work with all officially
supported operating systems and versions of
[.NET (Core)](https://dotnet.microsoft.com/download/dotnet),
and [.NET Framework](https://dotnet.microsoft.com/download/dotnet-framework)
except for versions lower than `.NET Framework 4.6.1`.

The code is automatically tested against following operating systems:

- Microsoft Windows Server 2019,
- macOS Catalina 10.15,
- Ubuntu 20.04 LTS.

## Usage

See [DEVELOPING.md](DEVELOPING.md) for build and running instructions.

See [USAGE.md](USAGE.md) for configuration instructions.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## Community Roles

Maintainers ([@open-telemetry/dotnet-instrumentation-maintainers](https://github.com/orgs/open-telemetry/teams/dotnet-instrumentation-maintainers)):

- [Chris Ventura](https://github.com/nrcventura), New Relic
- [Greg Paperin](https://github.com/macrogreg), Datadog
- [Paulo Janotti](https://github.com/pjanotti), Splunk
- [Zach Montoya](https://github.com/zacharycmontoya), Datadog

Approvers ([@open-telemetry/dotnet-instrumentation-approvers](https://github.com/orgs/open-telemetry/teams/dotnet-instrumentation-approvers)):

- [Colin Higgins](https://github.com/colin-higgins), Datadog
- [Kevin Gosse](https://github.com/kevingosse), Datadog
- [Lucas Pimentel-Ordyna](https://github.com/lucaspimentel), Datadog
- [Mike Goldsmith](https://github.com/MikeGoldsmith), HoneyComb
- [Robert Pajak](https://github.com/pellared), Splunk
- [Tony Redondo](https://github.com/tonyredondo), Datadog

Learn more about roles in the [community repository](https://github.com/open-telemetry/community/blob/main/community-membership.md).
