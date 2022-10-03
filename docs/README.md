# OpenTelemetry .NET Automatic Instrumentation

[![Slack](https://img.shields.io/badge/slack-@cncf/otel--dotnet--auto--instr-brightgreen.svg?logo=slack)](https://cloud-native.slack.com/archives/C01NR1YLSE7)

This project adds [OpenTelemetry instrumentation](https://opentelemetry.io/docs/concepts/instrumenting/#automatic-instrumentation)
to .NET applications without having to modify their source code.

---

⚠️ The following documentation refers to the in-development version
of OpenTelemetry .NET Automatic Instrumentation. Docs for the latest version
([0.3.1-beta.1](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/latest))
can be found [here](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v0.3.1-beta.1/docs/README.md).

---

OpenTelemetry .NET Automatic Instrumentation is built on top of
[OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet):

- [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
[`1.3.1`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.3.1)
- `System.Diagnostics.DiagnosticSource`: [`6.0.0`](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/6.0.0)
  referencing `System.Runtime.CompilerServices.Unsafe`: [`6.0.0`](https://www.nuget.org/packages/System.Runtime.CompilerServices.Unsafe/6.0.0)

You can find all references in
[OpenTelemetry.AutoInstrumentation.csproj](../src/OpenTelemetry.AutoInstrumentation/OpenTelemetry.AutoInstrumentation.csproj)
and [OpenTelemetry.AutoInstrumentation.AdditionalDeps/Directory.Build.props](../src/OpenTelemetry.AutoInstrumentation.AdditionalDeps/Directory.Build.props).

To automatically instrument applications, the OpenTelemetry .NET Automatic
Instrumentation does the following:

1. Injects and configures the [OpenTelemetry .NET SDK](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/README.md#opentelemetry-net-sdk)
   into the application.
2. Adds [OpenTelemetry Instrumentation](https://opentelemetry.io/docs/concepts/instrumenting/)
   to key packages and APIs used by the application.

You can enable the OpenTelemetry .NET Automatic Instrumentation as a .NET Profiler
to inject additional instrumentations of this project at runtime, using a technique
known as [monkey-patching](https://en.wikipedia.org/wiki/Monkey_patch). When enabled,
the OpenTelemetry .NET Automatic Instrumentation generates traces for libraries that
don't already generate traces using the OpenTelemetry .NET SDK.

See the [examples](../examples) for demonstrations of different instrumentation scenarios
covered by the OpenTelemetry .NET Automatic Instrumentation.

See [design.md](design.md) for an architectural overview.

## Status

This project is in the early stages of development.
[The project board](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/projects/1)
shows the current work in progress.

Project versioning information and stability guarantees
can be found in the [versioning documentation](versioning.md).

⚠️ **We need you!** ⚠️

Please, give us your **feedback** (in whatever form you like).

You can do this by [submitting a GitHub issue](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/new).

You may also prefer writing on [Slack](https://cloud-native.slack.com/archives/C01NR1YLSE7).
If you are new, you can create a CNCF Slack account [here](http://slack.cncf.io/).

See [CONTRIBUTING.md](CONTRIBUTING.md) for more information.

## Compatibility

OpenTelemetry .NET Automatic Instrumentation attempts to work with all officially
supported operating systems and versions of
[.NET (Core)](https://dotnet.microsoft.com/download/dotnet),
and [.NET Framework](https://dotnet.microsoft.com/download/dotnet-framework).

> Versions lower than `.NET Framework 4.6.2` are not supported.

CI tests run against the following operating systems:

- [macOS Big Sur 11](https://github.com/actions/runner-images/blob/main/images/macos/macos-11-Readme.md)
- [Microsoft Windows Server 2022](https://github.com/actions/runner-images/blob/main/images/win/Windows2022-Readme.md)
- [Ubuntu 20.04 LTS](https://github.com/actions/runner-images/blob/main/images/linux/Ubuntu2004-Readme.md)
- [Alpine](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/build/nuke/docker/alpine.dockerfile)

### Instrumented libraries and frameworks

See [config.md#instrumented-libraries-and-frameworks](config.md#instrumented-libraries-and-frameworks).

## Get started

### Install

Download and extract the appropriate binaries from
[the latest release](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/latest).

> The path where you put the binaries is referenced as `$INSTALL_DIR`

You can also use the [download.sh](../download.sh) script which uses following
environment variables as parameters:

| Parameter      | Description                                                      | Required | Default value                                                                     |
|----------------|------------------------------------------------------------------|----------|-----------------------------------------------------------------------------------|
| `DISTRIBUTION` | Possible values: `linux-glibc`, `linux-musl`, `macos`, `windows` | Yes      |                                                                                   |
| `INSTALL_DIR`  | Location where binaries are to be installed                      | No       | `./otel-dotnet-auto`                                                              |
| `RELEASES_URL` | GitHub releases URL                                              | No       | `https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases` |
| `TMPDIR`       | Temporary directory used when downloading the files              | No       | `$(mktemp -d)`                                                                    |
| `VERSION`      | Version to download                                              | No       | `v0.3.1-beta.1`                                                                   |

```sh
( set -o pipefail
curl -sSfL https://raw.githubusercontent.com/open-telemetry/opentelemetry-dotnet-instrumentation/main/download.sh |
  VERSION=v0.3.1-beta.1 DISTRIBUTION=linux-glibc bash -s )
```

### Instrument a .NET application

Before running your application, set the following environment variables:

```env
ASPNETCORE_HOSTINGSTARTUPASSEMBLIES=OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper
COR_ENABLE_PROFILING=1
COR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318}
CORECLR_ENABLE_PROFILING=1
CORECLR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318}
DOTNET_ADDITIONAL_DEPS=$INSTALL_DIR/AdditionalDeps
DOTNET_SHARED_STORE=$INSTALL_DIR/store
DOTNET_STARTUP_HOOKS=$INSTALL_DIR/netcoreapp3.1/OpenTelemetry.AutoInstrumentation.StartupHook.dll
OTEL_DOTNET_AUTO_HOME=$INSTALL_DIR
OTEL_DOTNET_AUTO_INTEGRATIONS_FILE=$INSTALL_DIR/integrations.json
```

On **Windows** you need to additionally set:

```env
COR_PROFILER_PATH_32=$INSTALL_DIR/win-x86/OpenTelemetry.AutoInstrumentation.Native.dll
COR_PROFILER_PATH_64=$INSTALL_DIR/win-x64/OpenTelemetry.AutoInstrumentation.Native.dll
CORECLR_PROFILER_PATH_32=$INSTALL_DIR/win-x86/OpenTelemetry.AutoInstrumentation.Native.dll
CORECLR_PROFILER_PATH_64=$INSTALL_DIR/win-x64/OpenTelemetry.AutoInstrumentation.Native.dll
```

On **Linux** you need to additionally set:

```env
CORECLR_PROFILER_PATH=$INSTALL_DIR/OpenTelemetry.AutoInstrumentation.Native.so
```

On **macOS** you need to additionally set:

```env
CORECLR_PROFILER_PATH=$INSTALL_DIR/OpenTelemetry.AutoInstrumentation.Native.dylib
```

Configure application's resources. For example:

```env
OTEL_SERVICE_NAME=my-service
OTEL_RESOURCE_ATTRIBUTES=deployment.environment=staging,service.version=1.0.0
```

On [.NET (Core)](https://dotnet.microsoft.com/download/dotnet),
if you don't need [bytecode instrumentations](config.md#instrumentations),
you can unset or remove the following environment variables
to not set the [.NET CLR Profiler](config.md#net-clr-profiler):

```env
COR_ENABLE_PROFILING
COR_PROFILER
COR_PROFILER_PATH_32
COR_PROFILER_PATH_64
CORECLR_ENABLE_PROFILING
CORECLR_PROFILER
CORECLR_PROFILER_PATH
CORECLR_PROFILER_PATH_32
CORECLR_PROFILER_PATH_64
OTEL_DOTNET_AUTO_INTEGRATIONS_FILE
```

You can also use the [instrument.sh](../instrument.sh) script which uses following
environment variables as parameters:

| Parameter          | Description                                                            | Required | Default value        |
|--------------------|------------------------------------------------------------------------|----------|----------------------|
| `DISTRIBUTION`     | Possible values: `linux-glibc`, `linux-musl`, `macos`, `windows`       | Yes      |                      |
| `ENABLE_PROFILING` | Whether to set the .NET CLR Profiler, possible values: `true`, `false` | No       | `true`               |
| `INSTALL_DIR`      | Location where binaries are to be installed                            | No       | `./otel-dotnet-auto` |

```sh
curl -fL https://raw.githubusercontent.com/open-telemetry/opentelemetry-dotnet-instrumentation/main/instrument.sh -O
DISTRIBUTION=linux-glibc source ./instrument.sh
OTEL_SERVICE_NAME=myapp dotnet run
```

## Instrument a Windows Service running a .NET application

See [windows-service-instrumentation.md](windows-service-instrumentation.md).

## Instrument an ASP.NET application deployed on IIS

See [iis-instrumentation.md](iis-instrumentation.md).

## Configuration

See [config.md](config.md).

### Manual instrumentation

See [manual-instrumentation.md](manual-instrumentation.md).

## Troubleshooting

See [troubleshooting.md](troubleshooting.md).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## Community Roles

[Maintainers](https://github.com/open-telemetry/community/blob/main/community-membership.md#maintainer)
([@open-telemetry/dotnet-instrumentation-maintainers](https://github.com/orgs/open-telemetry/teams/dotnet-instrumentation-maintainers)):

- [Chris Ventura](https://github.com/nrcventura), New Relic
- [Paulo Janotti](https://github.com/pjanotti), Splunk
- [Rajkumar Rangaraj](https://github.com/rajkumar-rangaraj), Microsoft
- [Robert Paj&#x105;k](https://github.com/pellared), Splunk
- [Zach Montoya](https://github.com/zacharycmontoya), Datadog

[Approvers](https://github.com/open-telemetry/community/blob/main/community-membership.md#approver)
([@open-telemetry/dotnet-instrumentation-approvers](https://github.com/orgs/open-telemetry/teams/dotnet-instrumentation-approvers)):

- [Piotr Kie&#x142;kowicz](https://github.com/Kielek), Splunk
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
