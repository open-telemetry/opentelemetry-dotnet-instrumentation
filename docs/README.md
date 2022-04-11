# OpenTelemetry .NET Auto-Instrumentation

[![Slack](https://img.shields.io/badge/slack-@cncf/otel--dotnet--auto--instr-brightgreen.svg?logo=slack)](https://cloud-native.slack.com/archives/C01NR1YLSE7)

This project adds [OpenTelemetry instrumentation](https://opentelemetry.io/docs/concepts/instrumenting/#automatic-instrumentation)
to .NET applications without having to modify their source code.

To auto-instrument applications, the instrumentation:

1. Injects and configures the [OpenTelemetry .NET SDK](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/README.md#opentelemetry-net-sdk) into the application.
2. Adds [OpenTelemetry Instrumentation](https://opentelemetry.io/docs/concepts/instrumenting/) to key packages and APIs used by the application.

The auto-instrumentation is capable of injecting instrumentations at runtime, 
a technique known as [monkey-patching](https://en.wikipedia.org/wiki/Monkey_patch). 
This allows to instrument specific packages or APIs that don't provide the necessary hooks
to generate .NET instrumentation packages.

See [DESIGN.md](DESIGN.md) for an architectural overview of the project.

## Status

This project is in the early stages of development.
[The project board](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/projects/1)
shows the current work in progress.

## Compatibility

OpenTelemetry .NET Auto-Instrumentation attempts to work with all officially
supported operating systems and versions of
[.NET (Core)](https://dotnet.microsoft.com/download/dotnet),
and [.NET Framework](https://dotnet.microsoft.com/download/dotnet-framework)
except for versions lower than `.NET Framework 4.6.2`.

CI tests run against the following operating systems:

- Microsoft Windows Server 2022,
- macOS Catalina 10.15,
- Ubuntu 20.04 LTS.

## Get Started

### Installation

Download and install the binaries from
[the latest release](https://github.com/open-telemetry/opentelemetry-dotnet/releases/latest).
In the docs, the binaries location is referred to as `%InstallationLocation%`.

On Linux, you can optionally create the default log directory
after installation by running the following commands:

```sh
sudo mkdir -p /var/log/opentelemetry/dotnet
sudo chmod a+rwx /var/log/opentelemetry/dotnet
```

### Instrument a .NET application on Windows

Before running the application, set the following environment variables:

```env
COR_ENABLE_PROFILING=1
COR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318}
COR_PROFILER_PATH_64=%InstallationLocation%/win-x64/OpenTelemetry.AutoInstrumentation.Native.dll
COR_PROFILER_PATH_32=%InstallationLocation%/win-x86/OpenTelemetry.AutoInstrumentation.Native.dll
CORECLR_ENABLE_PROFILING=1
CORECLR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318}
CORECLR_PROFILER_PATH_64=%InstallationLocation%/win-x64/OpenTelemetry.AutoInstrumentation.Native.dll
CORECLR_PROFILER_PATH_32=%InstallationLocation%/win-x86/OpenTelemetry.AutoInstrumentation.Native.dll
DOTNET_ADDITIONAL_DEPS=%InstallationLocation%/AdditionalDeps
DOTNET_SHARED_STORE=%InstallationLocation%/store
DOTNET_STARTUP_HOOKS=%InstallationLocation%/netcoreapp3.1/OpenTelemetry.AutoInstrumentation.StartupHook.dll
OTEL_DOTNET_AUTO_HOME=%InstallationLocation%
OTEL_DOTNET_AUTO_INTEGRATIONS_FILE=%InstallationLocation%/integrations.json
```

### Instrument a .NET application on Linux

Before running the application, set the following environment variables:

```env
CORECLR_ENABLE_PROFILING=1
CORECLR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318}
CORECLR_PROFILER_PATH=%InstallationLocation%/OpenTelemetry.AutoInstrumentation.Native.so
DOTNET_ADDITIONAL_DEPS=%InstallationLocation%/AdditionalDeps
DOTNET_SHARED_STORE=%InstallationLocation%/store
DOTNET_STARTUP_HOOKS=%InstallationLocation%/netcoreapp3.1/OpenTelemetry.AutoInstrumentation.StartupHook.dll
OTEL_DOTNET_AUTO_HOME=%InstallationLocation%
OTEL_DOTNET_AUTO_INTEGRATIONS_FILE=%InstallationLocation%/integrations.json
```

### Instrument a .NET application on macOS

Before running the application, set the following environment variables:

```env
CORECLR_ENABLE_PROFILING=1
CORECLR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318}
CORECLR_PROFILER_PATH=%InstallationLocation%/OpenTelemetry.AutoInstrumentation.Native.dylib
DOTNET_ADDITIONAL_DEPS=%InstallationLocation%/AdditionalDeps
DOTNET_SHARED_STORE=%InstallationLocation%/store
DOTNET_STARTUP_HOOKS=%InstallationLocation%/netcoreapp3.1/OpenTelemetry.AutoInstrumentation.StartupHook.dll
OTEL_DOTNET_AUTO_HOME=%InstallationLocation%
OTEL_DOTNET_AUTO_INTEGRATIONS_FILE=%InstallationLocation%/integrations.json
```

## Configuration

See [config.md](config.md).

## Troubleshooting

See [troubleshooting.md](troubleshooting.md).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## Community Roles

[Maintainers](https://github.com/open-telemetry/community/blob/main/community-membership.md#maintainer)
([@open-telemetry/dotnet-instrumentation-maintainers](https://github.com/orgs/open-telemetry/teams/dotnet-instrumentation-maintainers)):

- [Chris Ventura](https://github.com/nrcventura), New Relic
- [Paulo Janotti](https://github.com/pjanotti), Splunk
- [Robert Paj&#x105;k](https://github.com/pellared), Splunk
- [Zach Montoya](https://github.com/zacharycmontoya), Datadog

[Approvers](https://github.com/open-telemetry/community/blob/main/community-membership.md#approver)
([@open-telemetry/dotnet-instrumentation-approvers](https://github.com/orgs/open-telemetry/teams/dotnet-instrumentation-approvers)):

- [Rajkumar Rangaraj](https://github.com/rajkumar-rangaraj), Microsoft
- [Rasmus Kuusmann](https://github.com/RassK), Splunk

[Emeritus
Maintainer/Approver/Triager](https://github.com/open-telemetry/community/blob/main/community-membership.md#emeritus-maintainerapprovertriager):

- [Colin Higgins](https://github.com/colin-higgins), Datadog
- [Greg Paperin](https://github.com/macrogreg), Datadog
- [Kevin Gosse](https://github.com/kevingosse), Datadog
- [Lucas Pimentel-Ordyna](https://github.com/lucaspimentel), Datadog
- [Mike Goldsmith](https://github.com/MikeGoldsmith), HoneyComb
- [Tony Redondo](https://github.com/tonyredondo), Datadog

Learn more about roles in the [community repository](https://github.com/open-telemetry/community/blob/main/community-membership.md).
