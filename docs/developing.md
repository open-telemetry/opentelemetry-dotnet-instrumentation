# Development

## Development environment

On all platforms, the minimum requirements are:

- [Docker](https://www.docker.com/products/docker-desktop)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Windows

- [Visual Studio 2022 (17.4)](https://visualstudio.microsoft.com/downloads/)
  or higher
  - Workloads with the following components:
    - ASP.NET and web development
    - .NET desktop development
    - Desktop development with C++
  - Individual components:
    - MSVC v142 - Visual Studio 2019 C++ x64/x86 build tools (version 14.29)

Microsoft provides
[evaluation developer VMs](https://developer.microsoft.com/en-us/windows/downloads/virtual-machines)
with Windows and Visual Studio preinstalled.

### macOS

- Run: `xcode-select --install`

### Linux

- cmake, make, gcc, clang, clang++

### GitHub Codespaces

Run:

```sh
`./dev/codespaces-init.sh`
```

## Build

This repository uses [Nuke](https://nuke.build/) for build automation.

Support plugins are available for:

- JetBrains ReSharper        <https://nuke.build/docs/ide/resharper/>
- JetBrains Rider            <https://nuke.build/docs/ide/rider/>
- Microsoft VisualStudio     <https://nuke.build/docs/ide/visual-studio>
- Microsoft VSCode           <https://nuke.build/docs/ide/vscode/>

Restore dotnet tools to prepare build tools for solution.
This installs the dotnet `nuke` tool locally.

```cmd
dotnet tool restore
```

To see a list of possible targets and configurations run:

```cmd
dotnet nuke --help
```

To build, run:

```cmd
dotnet nuke
```

The main build artifacts are in `bin/tracer-home`.

Clean your repository by running:

```cmd
git clean -fXd
```

### Building NuGet packages locally

To build the NuGet package with the native components (`OpenTelemetry.AutoInstrumentation.Runtime.Native`)
locally it is necessary to download CI artifacts.

Download the `bin-*` artifacts from a successful CI job and expand each one into
a folder with the same name as the artifact under `./bin/ci-artifacts/`. The
PowerShell snippet below shows how to properly copy and expand the artifacts,
it assumes that the code is run from the root of the repository and the CI
artifacts we added to `~/Downloads/`:

```PowerShell
$artifacts = @(
    "bin-alpine-x64",
    "bin-alpine-arm64",
    "bin-ubuntu-22.04",
    "bin-ubuntu-22.04-arm",
    "bin-macos-14",
    "bin-windows-2022"
)
$destFolder = "./bin/ci-artifacts/"
$zipFilesFolder = "~/Downloads/"

rm -r -force $destFolder
mkdir $destFolder

$artifacts | % { $dest = $(Join-Path $destFolder $_); $zip = $(Join-Path $zipFilesFolder $_) + ".zip"; Expand-Archive $zip $dest }
```

Now you are ready to build the packages locally:

```cmd
dotnet nuke BuildNuGetPackages
```

to run the tests locally use:

```cmd
dotnet nuke TestNuGetPackages
```

To use the locally built NuGet packages in other projects on the local machine ensure
that the target project is either using a `nuget.config`, adding `<repo>/bin/nuget-artifacts/`
to the NuGet sources for example the
[`nuget.config` used by the NuGet packages test applications](../test/test-applications/nuget-packages/nuget.config),
or the packages are added to the project by specifying the `--source` parameter
when running [`dotnet add package` command](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-add-package).

Notice that package references are also cached so if you rebuild be sure to clean-up
the cached versions too.

### Documentation lint

If you made changes to the Markdown documents (`*.md` files), ensure that lint
tool and spellcheck passed without any issues by executing:

```cmd
nuke InstallDocumentationTools ValidateDocumentation
```

Some issues can be automatically fixed by:

```cmd
nuke MarkdownLintFix
```

All MarkdownLint tasks require [Node.js](https://nodejs.org/) installed locally.

### Managed code formatting

The .NET code formatting is based on the
[OpenTelemetry .NET repository](https://github.com/open-telemetry/opentelemetry-dotnet).

Installing formatter:

```sh
dotnet tool install -g dotnet-format
```

Formatting (Bash):

```sh
dotnet-format --folder
```

### Native code formatting

The C++ code formatting is based on the
[.NET Runtime repository](https://github.com/dotnet/runtime)
and [.NET JIT utils repository](https://github.com/dotnet/jitutils).

Installing formatter (Bash):

```sh
./scripts/download-clang-tools.sh
```

Formatting (Bash):

```sh
./scripts/format-native.sh
```

## Manual testing

### Test environment

The [`dev/docker-compose.yaml`](../dev/docker-compose.yaml) contains
configuration for running the OpenTelemetry Collector and Jaeger.

You can run the services using:

```sh
docker compose -f dev/docker-compose.yaml up
```

The following Web UI endpoints are exposed:

- <http://localhost:16686/search>: Collected traces
- <http://localhost:8889/metrics>: Collected metrics
- <http://localhost:13133>: Collector health status

You can also find the exported telemetry in `dev/log` directory.

### Instrument an application

> [!WARNING]
> Make sure to build and prepare the test environment beforehand.

You can reuse [`instrument.sh`](../instrument.sh) to export profiler
environmental variables to your current Shell session:

```sh
export OTEL_DOTNET_AUTO_HOME="bin/tracer-home"
. ./instrument.sh
```

The script can also launch the application to be instrumented directly:

```sh
OTEL_DOTNET_AUTO_HOME="bin/tracer-home" ./instrument.sh dotnet MyApp.dll
```

### Using playground application

You can use [the example playground application](../examples/playground)
to test the local changes.

## Release process

The release process is described in [releasing.md](releasing.md).

## Integration tests

Apart from regular unit tests this repository contains integration tests
under [test/IntegrationTests](../test/IntegrationTests)
as they give the biggest confidence if the automatic instrumentation works properly.

Each test class has its related test application that can be found
under [test/test-applications/integrations](../test/test-applications/integrations)
Each library instrumentation has its own test class.
Other features are tested via `SmokeTests` class or have its own test class
if a dedicated test application is needed.

Currently, the strategy is to test the library instrumentations
against following versions:

- its lowest supported, but not vulnerable, version,
- one version from every major release,
- the latest supported version (defined in [`test/Directory.Packages.props`](../test/Directory.Packages.props)),
- other specific versions, eg. containing breaking changes for our instrumentations.

Tests against these versions are executed when you are using `nuke` commands.
In case of execution from Visual Studio, only test against the latest supported
are executed.

To update set of the version modify [`PackageVersionDefinitions.cs`](../tools/LibraryVersionsGenerator/PackageVersionDefinitions.cs),
execute [`LibraryVersionsGenerator`](../tools/LibraryVersionsGenerator/LibraryVersionsGenerator.csproj),
and commit generated files.

> [!NOTE]
> `TestApplication.AspNet.NetFramework` is an exception to this strategy
> as it would not work well, because of multiple dependent packages.
> `TestApplication.AspNet.NetFramework` references the latest versions
> of the ASP.NET packages.

To verify that a test is not flaky,
you can [manually trigger](https://docs.github.com/en/actions/managing-workflow-runs/manually-running-a-workflow)
the [verify-test.yml](../.github/workflows/verify-test.yml) GitHub workflow.

## Debug the .NET runtime on Linux

- [Requirements](https://github.com/dotnet/runtime/blob/main/docs/workflow/requirements/linux-requirements.md)

- [Building .NET Runtime](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/libraries/README.md)

  ```bash
  ./build.sh clr+libs
  ```

- [Using `corerun`](https://github.com/dotnet/runtime/blob/main/docs/workflow/testing/using-corerun-and-coreroot.md)

  ```bash
  PATH="$PATH:$PWD/artifacts/bin/coreclr/Linux.x64.Debug/corerun"
  export CORE_LIBRARIES="$PWD/artifacts/bin/runtime/net6.0-Linux-Debug-x64"
  corerun ~/repos/opentelemetry-dotnet-instrumentation/examples/ConsoleApp/bin/Debug/net6.0/Examples.ConsoleApp.dll
  ```

- [Debugging](https://github.com/dotnet/runtime/blob/main/docs/workflow/debugging/coreclr/debugging-runtime.md)

  The following example shows how you can debug if the profiler is attached:

  ```bash
  ~/repos/opentelemetry-dotnet-instrumentation$ export OTEL_DOTNET_AUTO_HOME="bin/tracer-home"
  ~/repos/opentelemetry-dotnet-instrumentation$ . ./instrument.sh 
  ~/repos/opentelemetry-dotnet-instrumentation$ cd ../runtime/
  ~/repos/runtime$ lldb -- ./artifacts/bin/coreclr/Linux.x64.Debug/corerun ~/repos/opentelemetry-dotnet-instrumentation/examples/ConsoleApp/bin/Debug/net6.0/Examples.ConsoleApp.dll
  (lldb) target create "./artifacts/bin/coreclr/Linux.x64.Debug/corerun"
  Current executable set to '/home/user/repos/runtime/artifacts/bin/coreclr/Linux.x64.Debug/corerun' (x86_64).
  (lldb) settings set -- target.run-args  "/home/user/repos/opentelemetry-dotnet-instrumentation/examples/ConsoleApp/bin/Debug/net6.0/Examples.ConsoleApp.dll"
  (lldb) process launch -s
  Process 1905 launched: '/home/user/repos/runtime/artifacts/bin/coreclr/Linux.x64.Debug/corerun' (x86_64)
  (lldb) process handle -s false SIGUSR1 SIGUSR2
  NAME         PASS   STOP   NOTIFY
  ===========  =====  =====  ======
  SIGUSR1      true   false  true 
  SIGUSR2      true   false  true 
  (lldb) b EEToProfInterfaceImpl::CreateProfiler
  Breakpoint 1: no locations (pending).
  WARNING:  Unable to resolve breakpoint to any actual locations.
  (lldb) s
  Process 1905 stopped
  * thread #1, name = 'corerun', stop reason = instruction step into
      frame #0: 0x00007ffff7fd0103 ld-2.31.so
  ->  0x7ffff7fd0103: callq  0x7ffff7fd0df0            ; ___lldb_unnamed_symbol18$$ld-2.31.so
      0x7ffff7fd0108: movq   %rax, %r12
      0x7ffff7fd010b: movl   0x2c4e7(%rip), %eax
      0x7ffff7fd0111: popq   %rdx
  (lldb) c
  Process 1905 resuming
  1 location added to breakpoint 1
  Process 1905 stopped
  * thread #1, name = 'corerun', stop reason = breakpoint 1.1
      frame #0: 0x00007ffff7050ed2 libcoreclr.so`EEToProfInterfaceImpl::CreateProfiler(this=0x00005555555f7690, pClsid=0x00007fffffffce88, wszClsid=u"{918728DD-259F-4A6A-AC2B-B85E1B658318}", wszProfileDLL=u"/home/user/repos/opentelemetry-dotnet-instrumentation/bin/tracer-home/OpenTelemetry.AutoInstrumentation.Native.so") at eetoprofinterfaceimpl.cpp:633:5
    630      CONTRACTL_END;
    631 
    632      // Always called before Thread created.
  -> 633      _ASSERTE(GetThreadNULLOk() == NULL);
    634 
    635      // Try and CoCreate the registered profiler
    636      ReleaseHolder<ICorProfilerCallback2> pCallback2;
  (lldb) 
  ```

  You might need to add a [`dlerror()`](https://linux.die.net/man/3/dlerror) call
  in order to get the error message. For example:

  ```bash
  Process 20148 stopped
  * thread #1, name = 'corerun', stop reason = instruction step over
      frame #0: 0x00007ffff76166f8 libcoreclr.so`LOADLoadLibraryDirect(libraryNameOrPath="/home/user/repos/opentelemetry-dotnet-instrumentation/bin/tracer-home/OpenTelemetry.AutoInstrumentation.Native.so") at module.cpp:1477:9
    1474     if (dl_handle == nullptr)
    1475     {
    1476         LPCSTR err_msg = dlerror();
  -> 1477         TRACE("dlopen() failed %s\n", err_msg);
    1478         SetLastError(ERROR_MOD_NOT_FOUND);
    1479     }
    1480     else
  (lldb) var
  (LPCSTR) libraryNameOrPath = 0x00005555555f84c0 "/home/user/repos/opentelemetry-dotnet-instrumentation/bin/tracer-home/OpenTelemetry.AutoInstrumentation.Native.so"
  (NATIVE_LIBRARY_HANDLE) dl_handle = 0x0000000000000000
  (LPCSTR) err_msg = 0x00005555555f8740 "/home/user/repos/opentelemetry-dotnet-instrumentation/bin/tracer-home/OpenTelemetry.AutoInstrumentation.Native.so: undefined symbol: _binary_Datadog_Trace_ClrProfiler_Managed_Loader_pdb_end"  
  ```
