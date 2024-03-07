# OpenTelemetry .NET Automatic Instrumentation

[![Slack](https://img.shields.io/badge/slack-@cncf/otel--dotnet--auto--instr-brightgreen.svg?logo=slack)](https://cloud-native.slack.com/archives/C01NR1YLSE7)
[![NuGet](https://img.shields.io/nuget/v/OpenTelemetry.AutoInstrumentation.svg)](https://www.nuget.org/packages/OpenTelemetry.AutoInstrumentation)
[![NuGet](https://img.shields.io/nuget/dt/OpenTelemetry.AutoInstrumentation.svg)](https://www.nuget.org/packages/OpenTelemetry.AutoInstrumentation)
[![Arm CI sponsored by Actuated](https://img.shields.io/badge/SA_actuated.dev-004BDD)](https://actuated.dev/)

This project adds [OpenTelemetry instrumentation](https://opentelemetry.io/docs/concepts/instrumenting/#automatic-instrumentation)
to .NET applications without having to modify their source code.

---

> [!WARNING]
> The following documentation refers to the in-development version
of OpenTelemetry .NET Automatic Instrumentation. Docs for the latest version
([1.4.0](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/latest))
can be found in [opentelemetry.io](https://github.com/open-telemetry/opentelemetry.io/tree/main/content/en/docs/languages/net/automatic)
or [here](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v1.4.0/docs/README.md).

---

## Quick start

If you'd like to try the instrumentation on an existing application before
learning more about the configuration options and the project, follow the
instructions at [Using the OpenTelemetry.AutoInstrumentation NuGet packages](./using-the-nuget-packages.md#using-the-opentelemetryautoinstrumentation-nuget-packages)
or use the appropriate install script:

- On Linux and macOS, use the [shell scripts](#shell-scripts).
- On Windows, use the [PowerShell module](#powershell-module-windows).

To see the telemetry from your application directly on the standard output, set
the following environment variables to `true` before launching your application:

- `OTEL_DOTNET_AUTO_LOGS_CONSOLE_EXPORTER_ENABLED`
- `OTEL_DOTNET_AUTO_METRICS_CONSOLE_EXPORTER_ENABLED`
- `OTEL_DOTNET_AUTO_TRACES_CONSOLE_EXPORTER_ENABLED`

For a demo using `docker compose`, clone this repository and
follow the [examples/demo/README.md](../examples/demo/README.md).

## Components

OpenTelemetry .NET Automatic Instrumentation is built on top of
[OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet):

- [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
[`1.7.0`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.7.0)
- `System.Diagnostics.DiagnosticSource`: [`8.0.0`](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/8.0.0)
  referencing `System.Runtime.CompilerServices.Unsafe`: [`6.0.0`](https://www.nuget.org/packages/System.Runtime.CompilerServices.Unsafe/6.0.0)

You can find all references in
[OpenTelemetry.AutoInstrumentation.csproj](../src/OpenTelemetry.AutoInstrumentation/OpenTelemetry.AutoInstrumentation.csproj)
and [OpenTelemetry.AutoInstrumentation.AdditionalDeps/Directory.Build.props](../src/OpenTelemetry.AutoInstrumentation.AdditionalDeps/Directory.Build.props).

To automatically instrument applications, the OpenTelemetry .NET Automatic
Instrumentation does the following:

1. Injects and configures the [OpenTelemetry .NET SDK](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/README.md#opentelemetry-net-sdk)
   into the application.
1. Adds [OpenTelemetry Instrumentation](https://opentelemetry.io/docs/concepts/instrumenting/)
   to key packages and APIs used by the application.

You can enable the OpenTelemetry .NET Automatic Instrumentation as a .NET Profiler
to inject additional instrumentations of this project at runtime, using a technique
known as [monkey-patching](https://en.wikipedia.org/wiki/Monkey_patch). When enabled,
the OpenTelemetry .NET Automatic Instrumentation generates traces for libraries that
don't already generate traces using the OpenTelemetry .NET SDK.

See [design.md](design.md) for an architectural overview.

## Status

The versioning information and stability guarantees
can be found in the [versioning documentation](versioning.md).

## Compatibility

OpenTelemetry .NET Automatic Instrumentation attempts to work with all officially
supported operating systems and versions of
[.NET](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).

The minimal supported version of
[.NET Framework](https://dotnet.microsoft.com/download/dotnet-framework)
is `4.6.2`.

Supported processor architectures are:

- x86
- AMD64 (x86-64)
- ARM64 ([Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md))

> [!NOTE]
> ARM64 build does not support CentOS based images.

CI tests run against the following operating systems:

- [Alpine x64](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/docker/alpine.dockerfile)
- [Alpine ARM64](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/docker/alpine.dockerfile)
- [Debian x64](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/docker/debian.dockerfile)
- [Debian ARM64](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/docker/debian-arm64.dockerfile)
- [CentOS 7 x64](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/docker/centos-build.dockerfile)
  (.NET 8 is not supported)
- [macOS Big Sur 11 x64](https://github.com/actions/runner-images/blob/main/images/macos/macos-11-Readme.md)
- [Microsoft Windows Server 2022 x64](https://github.com/actions/runner-images/blob/main/images/windows/Windows2022-Readme.md)
- [Ubuntu 20.04 LTS x64](https://github.com/actions/runner-images/blob/main/images/ubuntu/Ubuntu2004-Readme.md)
- Ubuntu 22.04 LTS ARM64

### Instrumented libraries and frameworks

See [config.md#instrumented-libraries-and-frameworks](config.md#instrumented-libraries-and-frameworks).

## Get started

### Considerations on scope

Instrumenting [`self-contained`](https://learn.microsoft.com/en-us/dotnet/core/deploying/#publish-self-contained)
applications is supported through [NuGet packages](./using-the-nuget-packages.md).
Note that a `self-contained` application is
automatically generated in .NET 7+ whenever the `dotnet publish` or `dotnet build`
command is used with a Runtime Identifier (RID) parameter, for example when `-r`
or `--runtime` is used when running the command.

### Install

Download and extract the appropriate binaries from
[the latest release](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/latest).

> [!NOTE]
> The path where you put the binaries is referenced as `$INSTALL_DIR`

### Instrument a .NET application

When running your application, make sure to:

1. Set the [resources](config.md#resources).
1. Set the environment variables from the table below.

| Environment variable       | .NET version        | Value                                                                     |
|----------------------------|---------------------|---------------------------------------------------------------------------|
| `COR_ENABLE_PROFILING`     | .NET Framework      | `1`                                                                       |
| `COR_PROFILER`             | .NET Framework      | `{918728DD-259F-4A6A-AC2B-B85E1B658318}`                                  |
| `COR_PROFILER_PATH_32`     | .NET Framework      | `$INSTALL_DIR/win-x86/OpenTelemetry.AutoInstrumentation.Native.dll`       |
| `COR_PROFILER_PATH_64`     | .NET Framework      | `$INSTALL_DIR/win-x64/OpenTelemetry.AutoInstrumentation.Native.dll`       |
| `CORECLR_ENABLE_PROFILING` | .NET                | `1`                                                                       |
| `CORECLR_PROFILER`         | .NET                | `{918728DD-259F-4A6A-AC2B-B85E1B658318}`                                  |
| `CORECLR_PROFILER_PATH`    | .NET on Linux glibc | `$INSTALL_DIR/linux-x64/OpenTelemetry.AutoInstrumentation.Native.so`      |
| `CORECLR_PROFILER_PATH`    | .NET on Linux musl  | `$INSTALL_DIR/linux-musl-x64/OpenTelemetry.AutoInstrumentation.Native.so` |
| `CORECLR_PROFILER_PATH`    | .NET on macOS       | `$INSTALL_DIR/osx-x64/OpenTelemetry.AutoInstrumentation.Native.dylib`     |
| `CORECLR_PROFILER_PATH_32` | .NET on Windows     | `$INSTALL_DIR/win-x86/OpenTelemetry.AutoInstrumentation.Native.dll`       |
| `CORECLR_PROFILER_PATH_64` | .NET on Windows     | `$INSTALL_DIR/win-x64/OpenTelemetry.AutoInstrumentation.Native.dll`       |
| `DOTNET_ADDITIONAL_DEPS`   | .NET                | `$INSTALL_DIR/AdditionalDeps`                                             |
| `DOTNET_SHARED_STORE`      | .NET                | `$INSTALL_DIR/store`                                                      |
| `DOTNET_STARTUP_HOOKS`     | .NET                | `$INSTALL_DIR/net/OpenTelemetry.AutoInstrumentation.StartupHook.dll`      |
| `OTEL_DOTNET_AUTO_HOME`    | All versions        | `$INSTALL_DIR`                                                            |

> [!NOTE]
> Some settings can be omitted on .NET. For more information, see [config.md](config.md#net-clr-profiler).

### Shell scripts

You can install OpenTelemetry .NET Automatic Instrumentation
and instrument your .NET application using the provided Shell scripts.

> [!NOTE]
> On macOS [`coreutils`](https://formulae.brew.sh/formula/coreutils) is required.

Example usage:

```sh
# Download the bash script
curl -sSfL https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/download/v1.4.0/otel-dotnet-auto-install.sh -O

# Install core files
sh ./otel-dotnet-auto-install.sh

# Enable execution for the instrumentation script
chmod +x $HOME/.otel-dotnet-auto/instrument.sh

# Setup the instrumentation for the current shell session
. $HOME/.otel-dotnet-auto/instrument.sh

# Run your application with instrumentation
OTEL_SERVICE_NAME=myapp OTEL_RESOURCE_ATTRIBUTES=deployment.environment=staging,service.version=1.0.0 ./MyNetApp
```

`otel-dotnet-auto-install.sh` script
uses environment variables as parameters:

| Parameter               | Description                                                      | Required | Default value             |
|-------------------------|------------------------------------------------------------------|----------|---------------------------|
| `OTEL_DOTNET_AUTO_HOME` | Location where binaries are to be installed                      | No       | `$HOME/.otel-dotnet-auto` |
| `OS_TYPE`               | Possible values: `linux-glibc`, `linux-musl`, `macos`, `windows` | No       | *Calculated*              |
| `ARCHITECTURE`          | Possible values for Linux: `x64`, `arm64`                        | No       | *Calculated*              |
| `TMPDIR`                | Temporary directory used when downloading the files              | No       | `$(mktemp -d)`            |
| `VERSION`               | Version to download                                              | No       | `1.4.0`                   |

[instrument.sh](../instrument.sh) script
uses environment variables as parameters:

| Parameter               | Description                                                            | Required | Default value             |
|-------------------------|------------------------------------------------------------------------|----------|---------------------------|
| `ENABLE_PROFILING`      | Whether to set the .NET CLR Profiler, possible values: `true`, `false` | No       | `true`                    |
| `OTEL_DOTNET_AUTO_HOME` | Location where binaries are to be installed                            | No       | `$HOME/.otel-dotnet-auto` |
| `OS_TYPE`               | Possible values: `linux-glibc`, `linux-musl`, `macos`, `windows`       | No       | *Calculated*              |
| `ARCHITECTURE`          | Possible values for Linux: `x64`, `arm64`                              | No       | *Calculated*              |

### PowerShell module (Windows)

On Windows, you should install OpenTelemetry .NET Automatic Instrumentation
and instrument your .NET application using the provided PowerShell module.
Example usage (run as administrator):

```powershell
# Download the module
$module_url = "https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/download/v1.4.0/OpenTelemetry.DotNet.Auto.psm1"
$download_path = Join-Path $env:temp "OpenTelemetry.DotNet.Auto.psm1"
Invoke-WebRequest -Uri $module_url -OutFile $download_path -UseBasicParsing

# Import the module to use its functions
Import-Module $download_path

# Install core files (online vs offline method)
Install-OpenTelemetryCore
Install-OpenTelemetryCore -LocalPath "C:\Path\To\OpenTelemetry.zip" 

# Set up the instrumentation for the current PowerShell session
Register-OpenTelemetryForCurrentSession -OTelServiceName "MyServiceDisplayName"

# Run your application with instrumentation
.\MyNetApp.exe
```

You can get usage information by calling:

```powershell
# List all available commands
Get-Command -Module OpenTelemetry.DotNet.Auto

# Get command's usage information
Get-Help Install-OpenTelemetryCore -Detailed
```

Updating OpenTelemetry installation:

```powershell
# Import the previously downloaded module. After an update the module is found in the default install directory.
# Note: It's best to use the same version of the module for installation and uninstallation to ensure proper removal.
Import-Module "C:\Program Files\OpenTelemetry .NET AutoInstrumentation\OpenTelemetry.DotNet.Auto.psm1"

# If IIS was previously registered, use RegisterIIS = $true.
Update-OpenTelemetryCore -RegisterIIS $true

# If Windows services were previously registered, these must be re-registered manually.
Unregister-OpenTelemetryForWindowsService -WindowsServiceName MyServiceName
Update-OpenTelemetryCore
Register-OpenTelemetryForWindowsService -WindowsServiceName MyServiceName -OTelServiceName MyOtelServiceName
```

> [!WARNING]
> The PowerShell module works only on PowerShell 5.1
which is the one installed by default on Windows.

## Instrument a container

You can find our demonstrative example
that uses Docker Compose [here](../examples/demo).

You can also consider using
the [Kubernetes Operator for OpenTelemetry Collector](https://github.com/open-telemetry/opentelemetry-operator).

## Instrument a Windows Service running a .NET application

See [windows-service-instrumentation.md](windows-service-instrumentation.md).

## Instrument an ASP.NET application deployed on IIS

See [iis-instrumentation.md](iis-instrumentation.md).

## Configuration

See [config.md](config.md).

### Manual instrumentation

See [manual-instrumentation.md](manual-instrumentation.md).

## Log to trace correlation

See [log-trace-correlation.md](log-trace-correlation.md).

## Troubleshooting

See [troubleshooting.md](troubleshooting.md).

## Contact

See [CONTRIBUTING.md](CONTRIBUTING.md).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## Community Roles

[Maintainers](https://github.com/open-telemetry/community/blob/main/community-membership.md#maintainer)
([@open-telemetry/dotnet-instrumentation-maintainers](https://github.com/orgs/open-telemetry/teams/dotnet-instrumentation-maintainers)):

- [Chris Ventura](https://github.com/nrcventura), New Relic
- [Paulo Janotti](https://github.com/pjanotti), Splunk
- [Piotr Kie&#x142;kowicz](https://github.com/Kielek), Splunk
- [Rajkumar Rangaraj](https://github.com/rajkumar-rangaraj), Microsoft
- [Robert Paj&#x105;k](https://github.com/pellared), Splunk
- [Zach Montoya](https://github.com/zacharycmontoya), Datadog

[Approvers](https://github.com/open-telemetry/community/blob/main/community-membership.md#approver)
([@open-telemetry/dotnet-instrumentation-approvers](https://github.com/orgs/open-telemetry/teams/dotnet-instrumentation-approvers)):

- [Mateusz &#x141;ach](https://github.com/lachmatt), Splunk
- [Rasmus Kuusmann](https://github.com/RassK), Splunk

[Emeritus
Maintainer/Approver/Triager](https://github.com/open-telemetry/community/blob/main/community-membership.md#emeritus-maintainerapprovertriager):

- [Colin Higgins](https://github.com/colin-higgins)
- [Greg Paperin](https://github.com/macrogreg)
- [Kevin Gosse](https://github.com/kevingosse)
- [Lucas Pimentel-Ordyna](https://github.com/lucaspimentel)
- [Mike Goldsmith](https://github.com/MikeGoldsmith)
- [Tony Redondo](https://github.com/tonyredondo)

Learn more about roles in the [community repository](https://github.com/open-telemetry/community/blob/main/community-membership.md).
