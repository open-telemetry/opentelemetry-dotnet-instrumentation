# OpenTelemetry .NET Automatic Instrumentation

[![Slack](https://img.shields.io/badge/slack-@cncf/otel--dotnet--auto--instr-brightgreen.svg?logo=slack)](https://cloud-native.slack.com/archives/C01NR1YLSE7)
[![NuGet](https://img.shields.io/nuget/v/OpenTelemetry.AutoInstrumentation.svg)](https://www.nuget.org/packages/OpenTelemetry.AutoInstrumentation)
[![NuGet](https://img.shields.io/nuget/dt/OpenTelemetry.AutoInstrumentation.svg)](https://www.nuget.org/packages/OpenTelemetry.AutoInstrumentation)
[![OpenSSF Scorecard](https://api.scorecard.dev/projects/github.com/open-telemetry/opentelemetry-dotnet-instrumentation/badge)](https://scorecard.dev/viewer/?uri=github.com/open-telemetry/opentelemetry-dotnet-instrumentation)
[![OpenSSF Best Practices](https://www.bestpractices.dev/projects/10371/badge)](https://www.bestpractices.dev/projects/10371)
[![FOSSA License Status](https://app.fossa.com/api/projects/custom%2B162%2Fgithub.com%2Fopen-telemetry%2Fopentelemetry-dotnet-instrumentation.svg?type=shield&issueType=license)](https://app.fossa.com/projects/custom%2B162%2Fgithub.com%2Fopen-telemetry%2Fopentelemetry-dotnet-instrumentation?ref=badge_shield&issueType=license)
[![FOSSA Security Status](https://app.fossa.com/api/projects/custom%2B162%2Fgithub.com%2Fopen-telemetry%2Fopentelemetry-dotnet-instrumentation.svg?type=shield&issueType=security)](https://app.fossa.com/projects/custom%2B162%2Fgithub.com%2Fopen-telemetry%2Fopentelemetry-dotnet-instrumentation?ref=badge_shield&issueType=security)

This project adds [OpenTelemetry instrumentation](https://opentelemetry.io/docs/concepts/instrumentation/zero-code/)
to .NET applications without having to modify their source code.

---

> [!WARNING]
> The following documentation refers to the in-development version
of OpenTelemetry .NET Automatic Instrumentation. Docs for the latest version
([1.14.1](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/latest))
can be found in [opentelemetry.io](https://opentelemetry.io/docs/zero-code/dotnet/)
or [versioned README](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/v1.14.1/docs/README.md).

---

## Quick start

If you'd like to try the instrumentation on an existing application before
learning more about the configuration options and the project, use the
recommended installation method described at
[Using the OpenTelemetry.AutoInstrumentation NuGet packages](./using-the-nuget-packages.md#using-the-opentelemetryautoinstrumentation-nuget-packages)
or use the appropriate install script:

- On Linux and macOS, use the [shell scripts](#shell-scripts).
- On Windows, use the [PowerShell module](#powershell-module-windows).

> [!NOTE]
> The NuGet packages are the recommended way to deploy automatic instrumentation,
> but they can't be used in all cases. See [Limitations](./using-the-nuget-packages.md#limitations)
> for details.

To see the telemetry from your application directly on the standard output, set
the following environment variables to `console` before launching your application:

- `OTEL_TRACES_EXPORTER`
- `OTEL_METRICS_EXPORTER`
- `OTEL_LOGS_EXPORTER`

For a demo using `docker compose`, clone this repository and
follow the [examples/demo/README.md](../examples/demo/README.md).

## Components

OpenTelemetry .NET Automatic Instrumentation is built on top of
[OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet):

- [Core components](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/VERSIONING.md#core-components):
[`1.15.0`](https://github.com/open-telemetry/opentelemetry-dotnet/releases/tag/core-1.15.0)
- `System.Diagnostics.DiagnosticSource`: [`10.0.0`](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/10.0.0)
  referencing `System.Runtime.CompilerServices.Unsafe`: [`6.1.2`](https://www.nuget.org/packages/System.Runtime.CompilerServices.Unsafe/6.1.2)

You can find all references here:

- [OpenTelemetry.AutoInstrumentation.csproj](../src/OpenTelemetry.AutoInstrumentation/OpenTelemetry.AutoInstrumentation.csproj)
- [OpenTelemetry.AutoInstrumentation.Assemblies/Directory.Packages.props](../src/OpenTelemetry.AutoInstrumentation.Assemblies/Directory.Packages.props)

To automatically instrument applications, the OpenTelemetry .NET Automatic
Instrumentation does the following:

1. Injects and configures the [OpenTelemetry .NET SDK](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/README.md#opentelemetry-net-sdk)
   into the application.
1. Adds [OpenTelemetry Instrumentation](https://opentelemetry.io/docs/concepts/instrumenting/)
   to key packages and APIs used by the application.

You can enable the OpenTelemetry .NET Automatic Instrumentation as a .NET Profiler
to inject additional instrumentations of this project at runtime, using a technique
known as [monkey-patching](https://en.wikipedia.org/wiki/Monkey_patch). When enabled,
the OpenTelemetry .NET Automatic Instrumentation generates traces for libraries
that don't already generate traces using the OpenTelemetry .NET SDK.

See [design.md](design.md) for an architectural overview.

## Status

The versioning information and stability guarantees
can be found in the [versioning documentation](versioning.md).

## Compatibility

OpenTelemetry .NET Automatic Instrumentation should work with all officially
supported operating systems and versions of
[.NET](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).

The minimal supported version of
[.NET Framework](https://dotnet.microsoft.com/download/dotnet-framework)
is `4.6.2`.

Supported processor architectures are:

- x86
- AMD64 (x86-64)
- ARM64 ([Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md))

CI tests run against the following operating systems:

- [Alpine x64](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/docker/alpine.dockerfile)
- [Alpine ARM64](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/docker/alpine.dockerfile)
- [Debian x64](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/docker/debian.dockerfile)
- [Debian ARM64](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/docker/debian-arm64.dockerfile)
- [CentOS Stream 9 x64](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/docker/centos-stream9.dockerfile)
- [macOS Sonoma 14 ARM64](https://github.com/actions/runner-images/blob/main/images/macos/macos-14-Readme.md)
- [Microsoft Windows Server 2022 x64](https://github.com/actions/runner-images/blob/main/images/windows/Windows2022-Readme.md)
- [Microsoft Windows Server 2025 x64](https://github.com/actions/runner-images/blob/main/images/windows/Windows2025-Readme.md)
- [Ubuntu 22.04 LTS x64](https://github.com/actions/runner-images/blob/main/images/ubuntu/Ubuntu2204-Readme.md)
- [Ubuntu 22.04 LTS ARM64](https://github.com/actions/partner-runner-images/blob/main/images/arm-ubuntu-22-image.md)

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

### Install using NuGet packages

The NuGet packages are the recommended way to deploy automatic instrumentation,
but they can't be used in all cases. To install using the NuGet packages,
see [Using the OpenTelemetry.AutoInstrumentation NuGet packages](./using-the-nuget-packages.md).
See [Limitations](./using-the-nuget-packages.md#limitations) for incompatible scenarios.

### Install manually

To install the automatic instrumentation manually, download and extract the
appropriate binaries from [the latest release](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/latest).

> [!NOTE]
> The path where you put the binaries is referenced as `$INSTALL_DIR`.

### Instrument a .NET application

When running your application, make sure to:

1. Set the [resources](config.md#resources).
1. Set the environment variables from the table below.

> [!NOTE]
> Some settings can be omitted on .NET. For more information, see [config.md](config.md#net-clr-profiler).

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
| `CORECLR_PROFILER_PATH`    | .NET on macOS       | `$INSTALL_DIR/osx-arm64/OpenTelemetry.AutoInstrumentation.Native.dylib`   |
| `CORECLR_PROFILER_PATH_32` | .NET on Windows     | `$INSTALL_DIR/win-x86/OpenTelemetry.AutoInstrumentation.Native.dll`       |
| `CORECLR_PROFILER_PATH_64` | .NET on Windows     | `$INSTALL_DIR/win-x64/OpenTelemetry.AutoInstrumentation.Native.dll`       |
| `DOTNET_STARTUP_HOOKS`     | .NET                | `$INSTALL_DIR/net/OpenTelemetry.AutoInstrumentation.StartupHook.dll`      |
| `OTEL_DOTNET_AUTO_HOME`    | All versions        | `$INSTALL_DIR`                                                            |

> [!IMPORTANT]
> Starting in .NET 8, the environment variable `DOTNET_EnableDiagnostics=0`
disables all diagnostics, including the CLR Profiler facility which is needed
to launch the instrumentation, if not using .NET Startup hooks. Ensure that
`DOTNET_EnableDiagnostics=1`, or if you'd like to limit diagnostics only to
the CLR Profiler, you may set both `DOTNET_EnableDiagnostics=1` and
`DOTNET_EnableDiagnostics_Profiler=1` while setting other diagnostics features
to 0. See this [issue](https://github.com/dotnet/runtime/issues/96227#issuecomment-1865326080)
for more guidance.

### Shell scripts

You can install OpenTelemetry .NET Automatic Instrumentation
and instrument your .NET application using the provided Shell scripts.

> [!NOTE]
> On macOS [`coreutils`](https://formulae.brew.sh/formula/coreutils) is required.

Example usage:

```sh
# Download the bash script
curl -sSfL https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/download/v1.13.0/otel-dotnet-auto-install.sh -O

# Install core files
sh ./otel-dotnet-auto-install.sh

# Enable execution for the instrumentation script
chmod +x $HOME/.otel-dotnet-auto/instrument.sh

# Setup the instrumentation for the current shell session
. $HOME/.otel-dotnet-auto/instrument.sh

# Run your application with instrumentation
OTEL_SERVICE_NAME=myapp OTEL_RESOURCE_ATTRIBUTES=deployment.environment.name=staging,service.version=1.0.0 ./MyNetApp
```

NOTE: for air-gapped environments you can provide either the installation
archive directly with:

```sh
LOCAL_PATH=<PATH_TO_ARCHIVE> sh ./otel-dotnet-auto-install.sh
```

or the folder with the archives, this has the added benefit that the install
script will determine the correct archive to choose.

```sh
DOWNLOAD_DIR=<PATH_TO_FOLDER_WITH_ARCHIVES> sh ./otel-dotnet-auto-install.sh
```

`otel-dotnet-auto-install.sh` script
uses environment variables as parameters:

| Parameter               | Description                                                                     | Required | Default value               |
|-------------------------|---------------------------------------------------------------------------------|----------|-----------------------------|
| `OTEL_DOTNET_AUTO_HOME` | Location where binaries are to be installed                                     | No       | `$HOME/.otel-dotnet-auto`   |
| `OS_TYPE`               | Possible values: `linux-glibc`, `linux-musl`, `macos`, `windows`                | No       | *Calculated*                |
| `ARCHITECTURE`          | Possible values for Linux: `x64`, `arm64`                                       | No       | *Calculated*                |
| `TMPDIR`                | (deprecated) prefer `DOWNLOAD_DIR`                                              | No       | `$(mktemp -d)`              |
| `DOWNLOAD_DIR`          | Folder to download the archive to. Will use local archive if it already exists  | No       | `$TMPDIR` or `$(mktemp -d)` |
| `LOCAL_PATH`            | Full path the archive to use for installation. (ideal for air-gapped scenarios) | No       | *Calculated*                |
| `VERSION`               | Version to download                                                             | No       | `1.14.1`                    |

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

> [!WARNING]
> The PowerShell module works only on PowerShell 5.1
which is the one installed by default on Windows.

Example usage (run as administrator):

```powershell
# PowerShell 5.1 is required
#Requires -PSEdition Desktop

# Download the module
$module_url = "https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/download/v1.14.1/OpenTelemetry.DotNet.Auto.psm1"
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

Uninstalling OpenTelemetry:

```powershell
# PowerShell 5.1 is required
#Requires -PSEdition Desktop

# Import the previously downloaded module. After installation or an update the module is found in the default install directory.
# Note: It's best to use the same version of the module for installation and uninstallation to ensure proper removal.
Import-Module "C:\Program Files\OpenTelemetry .NET AutoInstrumentation\OpenTelemetry.DotNet.Auto.psm1"

# If IIS was previously registered, unregister it.
Unregister-OpenTelemetryForIIS

# If Windows services were previously registered, unregister them.
Unregister-OpenTelemetryForWindowsService -WindowsServiceName MyServiceName

# Finally, uninstall OpenTelemetry instrumentation
Uninstall-OpenTelemetryCore
```

#### Update .NET Framework version

By default, `Install-OpenTelemetryCore` and `Update-OpenTelemetryCore` register
OpenTelemetry (and dependencies) assemblies in the Global Assembly Cache (GAC).
Some of these assemblies are tightly coupled to specific .NET Framework versions.

When upgrading from .NET Framework versions older than 4.7.2, these assemblies
should be removed from the GAC. For such upgrade scenarios, it is recommended
to uninstall and reinstall OpenTelemetry after the .NET Framework update is complete.

## Instrument a container

You can find our demonstrative example
that uses [Docker Compose](../examples/demo).

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

### Configuration based instrumentation

See [nocode-instrumentation.md](nocode-instrumentation.md)

## Log to trace correlation

See [log-trace-correlation.md](log-trace-correlation.md).

## Troubleshooting

See [troubleshooting.md](troubleshooting.md).

## Contact

See [CONTRIBUTING.md](CONTRIBUTING.md).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## Community Roles

### Maintainers

- [Chris Ventura](https://github.com/nrcventura), New Relic
- [Piotr Kie&#x142;kowicz](https://github.com/Kielek), Splunk
- [Rajkumar Rangaraj](https://github.com/rajkumar-rangaraj), Microsoft
- [Robert Paj&#x105;k](https://github.com/pellared), Splunk
- [Zach Montoya](https://github.com/zacharycmontoya), Datadog

For more information about the maintainer role, see the [community repository](https://github.com/open-telemetry/community/blob/main/community-membership.md#maintainer).

### Approvers

- [Mateusz &#x141;ach](https://github.com/lachmatt), Splunk
- [Rasmus Kuusmann](https://github.com/RassK), Splunk

For more information about the approver role, see the [community repository](https://github.com/open-telemetry/community/blob/main/community-membership.md#approver).

### Emeritus Maintainer/Approver/Triager

- [Colin Higgins](https://github.com/colin-higgins)
- [Greg Paperin](https://github.com/macrogreg)
- [Kevin Gosse](https://github.com/kevingosse)
- [Lucas Pimentel-Ordyna](https://github.com/lucaspimentel)
- [Mike Goldsmith](https://github.com/MikeGoldsmith)
- [Paulo Janotti](https://github.com/pjanotti)
- [Tony Redondo](https://github.com/tonyredondo)

For more information about the emeritus role, see the [community repository](https://github.com/open-telemetry/community/blob/main/guides/contributor/membership.md#emeritus-maintainerapprovertriager).

## Attestation

Starting with the `1.14.0` release the files included in the GitHub releases
are attested using [GitHub Artifact attestations](https://docs.github.com/actions/concepts/security/artifact-attestations).

To verify the attestation of a file from a GitHub release use the [GitHub CLI](https://cli.github.com/).

For example:

```bash
gh attestation verify --owner open-telemetry ./otel-dotnet-auto-install.sh
```

> [!NOTE]
> A successful verification outputs `Verification succeeded!`.

This repository also uses
[GitHub Immutable Releases](https://docs.github.com/code-security/concepts/supply-chain-security/immutable-releases)
which can also be verified.

For example:

```bash
RELEASE_TAG="v1.14.0"
gh release verify "${RELEASE_TAG}" --repo open-telemetry/opentelemetry-dotnet-instrumentation
gh release verify-asset "${RELEASE_TAG}" ./otel-dotnet-auto-install.sh --repo open-telemetry/opentelemetry-dotnet-instrumentation
```

> [!NOTE]
> A successful verification outputs `Release <tag> verified!`.

For more verification options please refer to the documentation for
[`gh attestation verify`](https://cli.github.com/manual/gh_attestation_verify),
[`gh release verify`](https://cli.github.com/manual/gh_release_verify),
and [`gh release verify-asset`](https://cli.github.com/manual/gh_release_verify-asset).
