# Development

## Windows

### Minimum requirements

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
- Optional: [.NET Core 3.0 Runtime](https://dotnet.microsoft.com/download/dotnet-core/3.0) to test in .NET Core 3.0 locally.
- Optional: [.NET Core 2.1 Runtime](https://dotnet.microsoft.com/download/dotnet-core/2.1) to test in .NET Core 2.1 locally.
- Optional: [nuget.exe CLI](https://www.nuget.org/downloads) v5.3 or newer
- Optional: [WiX Toolset 3.11.1](http://wixtoolset.org/releases/) or newer to build Windows installer (msi)
  - Requires .NET Framework 3.5 SP2 (install from Windows Features control panel: `OptionalFeatures.exe`)
  - [WiX Toolset Visual Studio Extension](https://wixtoolset.org/releases/) to build installer from Visual Studio
- Optional: [Docker for Windows](https://docs.docker.com/docker-for-windows/) to build Linux binaries and run integration tests on Linux containers. See [section on Docker Compose](#building-and-running-tests-with-docker-compose).
  - Requires Windows 10 (1607 Anniversary Update, Build 14393 or newer)

Microsoft provides [evaluation developer VMs](https://developer.microsoft.com/en-us/windows/downloads/virtual-machines) with Windows 10 and Visual Studio pre-installed.

### Building from a command line

From a _Developer Command Prompt for VS 2019_:

```cmd
rem Restore NuGet packages
rem nuget.exe is required for command line restore because msbuild doesn't support packages.config
rem (see https://github.com/NuGet/Home/issues/7386)
nuget restore Datadog.Trace.sln

rem Build C# projects (Platform: always AnyCPU)
msbuild Datadog.Trace.proj /t:BuildCsharp /p:Configuration=Release

rem Build NuGet packages
dotnet pack src\Datadog.Trace\Datadog.Trace.csproj
dotnet pack src\Datadog.Trace.OpenTracing\Datadog.Trace.OpenTracing.csproj

rem Build C++ projects
rem The native profiler depends on the Datadog.Trace.ClrProfiler.Managed.Loader C# project so be sure that is built first
msbuild Datadog.Trace.proj /t:BuildCpp /p:Configuration=Release;Platform=x64
msbuild Datadog.Trace.proj /t:BuildCpp /p:Configuration=Release;Platform=x86

rem Build MSI installer for Windows x64 (supports both x64 and x86 apps)
msbuild Datadog.Trace.proj /t:msi /p:Configuration=Release;Platform=x64

rem Build MSI installer for Windows x86 (supports x86 apps only)
msbuild Datadog.Trace.proj /t:msi /p:Configuration=Release;Platform=x86

rem Build tracer home directory for Windows.
rem Valid values for property `Platform` are `x64`, `x86`, and `All`.
msbuild Datadog.Trace.proj /t:CreateHomeDirectory /p:Configuration=Release;Platform=All
```

## Linux and MacOS

### Minimum requirements

To build C# projects and NuGet packages only
- [.NET 5.0 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)
- [.NET Core 3.1 Runtime](https://dotnet.microsoft.com/download/dotnet-core/3.1)
- Optional: [.NET Core 3.0 Runtime](https://dotnet.microsoft.com/download/dotnet-core/3.0) to test in .NET Core 3.0 locally.
- Optional: [.NET Core 2.1 Runtime](https://dotnet.microsoft.com/download/dotnet-core/2.1) to test in .NET Core 2.1 locally.

To build everything and run integration tests
- [Docker](https://docs.docker.com/engine/install/)
- [Docker Compose](https://docs.docker.com/compose/install/)

### Building

```sh
QUICK_BUILD=1 ./build/docker/build.sh
./build/docker/Datadog.Trace.ClrProfiler.Native.sh
```

## Visual Studio Code

This repository contains example configuration for VS Code located under `.vscode.example`. You can copy it to `.vscode`.

```sh
cp -r .vscode.example .vscode
```

### OmniSharp issues

Because of [Mono missing features](https://github.com/OmniSharp/omnisharp-vscode#note-about-using-net-5-sdks), `omnisharp.useGlobalMono` has to be set to `never`. Go to `File` -> `Preferences` -> `Settings` -> `Extensions` -> `C# Configuration` -> Change `Omnisharp: Use Global Mono` (you can search for it if the menu is too long) to `never`. Afterwards, you have restart OmniSharp: `F1` -> `OmniSharp: Restart OmniSharp`.

There may be a lot of errors, because some projects target .NET Framework. Switch to `Datadog.Trace.Minimal.sln` using `F1` -> `OmniSharp: Select Project` in Visual Studio Code to load a subset of projects which work without any issues. You can also try building the projects which have errors as it sometimes helps.

If for whatever reason you need to use `Datadog.Trace.sln` you can run `for i in **/*.csproj; do dotnet build $i; done` to decrease the number of errors.

## Development Container

The repository also contains configuration for developing inside a Container ([installation steps](https://code.visualstudio.com/docs/remote/containers#_installation)) using [Visual Studio Code Remote - Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) located under `.devcontainer.example`. You can copy it to `.devcontainer`.

```sh
cp -r .devcontainer.example .devcontainer
```

The Development Container configuration mixes [Docker in Docker](https://github.com/microsoft/vscode-dev-containers/tree/master/containers/docker-in-docker) and [C# (.NET)](https://github.com/microsoft/vscode-dev-containers/tree/master/containers/dotnet) definitions. Thanks to it you can use `docker` and `docker-compose` inside the container.

## Integration tests

You can use [Docker Compose](https://docs.docker.com/compose/) with Linux containers to build Linux binaries and run the test suites. This works on both Windows, Linux and MacOS hosts.

```bash
# build C# projects
docker-compose run build

# build C++ project
docker-compose run Profiler

# run integration tests
docker-compose run IntegrationTests
```

## Testing environment

The [`dev/docker-compose.yaml`](../dev/docker-compose.yaml) contains configuration for running OTel Collector and Jaeger.

You can run the services using from by:

```sh
docker-compose -f dev/docker-compose.yaml up
```

The following Web UI endpoints are exposed:
- http://localhost:16686/search - collected traces,
- http://localhost:8889/metrics - collected metrics,
- http://localhost:13133 - collector's health.

## Instrumentation Scripts

> *Caution:* Make sure that before usage you have build the tracer.

[`dev/instrument.sh`](../dev/instrument.sh) helps to run a command with .NET instrumentation in your shell (e.g. bash, zsh, git bash) .

Example usage:

```sh
./dev/instrument.sh dotnet run -f netcoreapp3.1 -p ./samples/ConsoleApp/ConsoleApp.csproj
```

 [`dev/envvars.sh`](../dev/envvars.sh) can be used to export profiler environmental variables to your current shell session. **It has to be executed from the root of this repository**. Example usage:

 ```sh
 source ./dev/envvars.sh
 ./samples/ConsoleApp/bin/Debug/netcoreapp3.1/ConsoleApp
 ```
