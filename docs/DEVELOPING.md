# Development

## Windows

Minimum requirements:

- [Visual Studio 2019 (16.8)](https://visualstudio.microsoft.com/downloads/) or newer
  - Workloads
    - Desktop development with C++
    - .NET desktop development
    - .NET Core cross-platform development
    - Optional: ASP.NET and web development (to build samples)
  - Individual components
    - .NET Framework 4.7 targeting pack
- [.NET 5.0 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)
- [.NET Core 3.1 Runtime](https://dotnet.microsoft.com/download/dotnet-core/3.1)
- Optional: [nuget.exe CLI](https://www.nuget.org/downloads) v5.3 or newer
- Optional: [WiX Toolset 3.11.1](http://wixtoolset.org/releases/) or newer to build Windows installer (msi)
  - Requires .NET Framework 3.5 SP2 (install from Windows Features control panel: `OptionalFeatures.exe`)
  - [WiX Toolset Visual Studio Extension](https://wixtoolset.org/releases/) to build installer from Visual Studio
- Optional: [Docker for Windows](https://docs.docker.com/docker-for-windows/) to build Linux binaries and run integration tests on Linux containers. See [section on Docker Compose](#building-and-running-tests-with-docker-compose).
  - Requires Windows 10 (1607 Anniversary Update, Build 14393 or newer)

Microsoft provides [evaluation developer VMs](https://developer.microsoft.com/en-us/windows/downloads/virtual-machines) with Windows 10 and Visual Studio pre-installed.

## Linux and MacOS

Minimum requirements:

- [.NET 5.0 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)
- [.NET Core 3.1 Runtime](https://dotnet.microsoft.com/download/dotnet-core/3.1)

To build everything and run integration tests:

- [Docker](https://docs.docker.com/engine/install/)
- [Docker Compose](https://docs.docker.com/compose/install/)

### Visual Studio Code - OmniSharp issues

Because of [Mono missing features](https://github.com/OmniSharp/omnisharp-vscode#note-about-using-net-5-sdks), `omnisharp.useGlobalMono` has to be set to `never`. Go to `File` -> `Preferences` -> `Settings` -> `Extensions` -> `C# Configuration` -> Change `Omnisharp: Use Global Mono` (you can search for it if the menu is too long) to `never`. Afterwards, you have restart OmniSharp: `F1` -> `OmniSharp: Restart OmniSharp`.

There may be a lot of errors, because some projects target .NET Framework. Switch to `OpenTelemetry.ClrProfiler.sln` using `F1` -> `OmniSharp: Select Project` in Visual Studio Code to load a subset of projects which work without any issues. You can also try building the projects which have errors as it sometimes helps.

If for whatever reason you need to use `OpenTelemetry.ClrProfiler.sln` you can run `dotnet nuke` to decrease the number of errors.

## Build

This repository uses [Nuke](https://nuke.build/) for build automation.

Support plugins are available for:

- JetBrains ReSharper        https://nuke.build/resharper
- JetBrains Rider            https://nuke.build/rider
- Microsoft VisualStudio     https://nuke.build/visualstudio
- Microsoft VSCode           https://nuke.build/vscode

Restore dotnet tools to prepare build tools for solution. This will install dotnet nuke tool locally.

```cmd
dotnet tool restore
```

To see a list of possible targets and configurations run:

```cmd
dotnet nuke --help
```

To build the tracer you can simply run:

```cmd
dotnet nuke
```

Clean your repository by running:

```cmd
git clean -fXd
```

## Test Environment

The [`dev/docker-compose.yaml`](../dev/docker-compose.yaml) contains configuration for running OTel Collector and Jaeger.

You can run the services using:

```sh
docker-compose -f dev/docker-compose.yaml up
```

The following Web UI endpoints are exposed:

- <http://localhost:16686/search> - collected traces,
- <http://localhost:8889/metrics> - collected metrics,
- <http://localhost:13133> - collector's health.

## Instrument applications

> *Caution:* Make sure that before usage you have build the tracer.

[`dev/instrument.sh`](../dev/instrument.sh) helps to run a command with .NET instrumentation in your shell (e.g. bash, zsh, git bash).

Example usage:

```sh
./dev/instrument.sh dotnet run -f netcoreapp3.1 -p ./samples/ConsoleApp/ConsoleApp.csproj
```

 [`dev/envvars.sh`](../dev/envvars.sh) can be used to export profiler environmental variables to your current shell session.
 **It has to be executed from the root of this repository**. Example usage:

 ```sh
 source ./dev/envvars.sh
 ./samples/ConsoleApp/bin/Debug/netcoreapp3.1/ConsoleApp
 ```
