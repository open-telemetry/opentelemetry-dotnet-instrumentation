# OpenTelemetry .NET Auto-Instrumentation

[![Slack](https://img.shields.io/badge/slack-@cncf/otel--dotnet--auto--instr-brightgreen.svg?logo=slack)](https://cloud-native.slack.com/archives/C01NR1YLSE7)

This project aims to add [OpenTelemetry Instrumentation](https://opentelemetry.io/docs/concepts/instrumenting/#automatic-instrumentation)
to .NET applications without requiring any changes to their source code.
To do that, it must:

1. Inject and setup the [OpenTelemetry .NET SDK](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/README.md#opentelemetry-net-sdk) into the application;
2. Add [OpenTelmetry Instrumentation](https://opentelemetry.io/docs/concepts/instrumenting/) to key packages and APIs used by the application;

Moreover, if a package or API doesn't provide the necessary hooks
to create a corresponding .NET instrumentation package,
this project offers the capability to inject instrumentations during the application runtime,
aka [monkey-patching](https://en.wikipedia.org/wiki/Monkey_patch) instrumentation.

See the [DESIGN.md](DESIGN.md) for an architectural overview of the project.

## Status

This project is in the early stages of development, starting with an initial seeding of code from the [.NET Tracer for Datadog APM](https://github.com/DataDog/dd-trace-dotnet). A [project board](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/projects/1) shows the current work in progress and the backlog.


## Compatibility

OpenTelemetry .NET Auto-Instrumentation attempts to work with all officially
supported operating systems and versions of
[.NET (Core)](https://dotnet.microsoft.com/download/dotnet),
and [.NET Framework](https://dotnet.microsoft.com/download/dotnet-framework)
except for versions lower than `.NET Framework 4.6.1`.

CI tests run against the following operating systems:

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
- [Robert Pajak](https://github.com/pellared), Splunk
- [Zach Montoya](https://github.com/zacharycmontoya), Datadog

Approvers ([@open-telemetry/dotnet-instrumentation-approvers](https://github.com/orgs/open-telemetry/teams/dotnet-instrumentation-approvers)):

- [Colin Higgins](https://github.com/colin-higgins), Datadog
- [Kevin Gosse](https://github.com/kevingosse), Datadog
- [Lucas Pimentel-Ordyna](https://github.com/lucaspimentel), Datadog
- [Mike Goldsmith](https://github.com/MikeGoldsmith), HoneyComb
- [Rajkumar Rangaraj](https://github.com/rajkumar-rangaraj), Microsoft
- [Rasmus Kuusmann](https://github.com/RassK), Splunk
- [Tony Redondo](https://github.com/tonyredondo), Datadog

Learn more about roles in the [community repository](https://github.com/open-telemetry/community/blob/main/community-membership.md).
