# Development

## Development environment

On all platforms, the minimum requirements are:

- [Docker](https://docs.docker.com/engine/install/)
- [Docker Compose](https://docs.docker.com/compose/install/)
- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- [.NET 7.0 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)

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

- JetBrains ReSharper        <https://nuke.build/resharper>
- JetBrains Rider            <https://nuke.build/rider>
- Microsoft VisualStudio     <https://nuke.build/visualstudio>
- Microsoft VSCode           <https://nuke.build/vscode>

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

## Maintain integrations.json

[`integrations.json`](../integrations.json) can be regenerated using
[`Integrations Json Generator`](../tools/IntegrationsJsonGenerator) project.

To update this file you should

1. Modify bytecode instrumentation (especially [`InstrumentMethodAttribute`](../src/OpenTelemetry.AutoInstrumentation/Instrumentations/InstrumentMethodAttribute.cs)s).
1. Execute `nuke BuildTracer` to generate auto instrumentation library - source
   for the generator.
1. Execute [`Integrations Json Generator`](../tools/IntegrationsJsonGenerator).
1. Execute `nuke BuildTracer` to apply changes in `integrations.json`.

Remember to commit changes in `integrations.json`.

## Manual testing

### Test environment

The [`dev/docker-compose.yaml`](../dev/docker-compose.yaml) contains
configuration for running the OpenTelemetry Collector and Jaeger.

You can run the services using:

```sh
docker-compose -f dev/docker-compose.yaml up
```

The following Web UI endpoints are exposed:

- <http://localhost:16686/search>: Collected traces
- <http://localhost:8889/metrics>: Collected metrics
- <http://localhost:13133>: Collector health status

You can also find the exported telemetry in `dev/log` directory.

### Instrument an application

> *Warning:* Make sure to build and prepare the test environment beforehand.

You can use [`dev/envvars.sh`](../dev/envvars.sh) to export profiler
environmental variables to your current shell session.
You must run it from the root of this repository.
For example:

```sh
. ./dev/envvars.sh
./test/test-applications/integrations/TestApplication.Smoke/bin/x64/Release/net7.0/TestApplication.Smoke
```

### Using playground application

You can use [the example playground application](../examples/playground)
to test the local changes.

## Release process

The release process is described in [releasing.md](releasing.md).

## Integration tests

Apart from regular unit tests this repository contains integration tests
under [test/IntegrationTests](../test/IntegrationTests)
as they give the biggest confidence if the auto-instrumentation works properly.

Each test class has its related test application that can be found
under [test/test-applications/integrations](../test/test-applications/integrations)
Each library instrumentation has its own test class.
Other features are tested via `SmokeTests` class or have its own test class
if a dedicated test application is needed.

Currently, the strategy is to test the library instrumentations
against its lowest supported, but not vulnerable, version.
The pull requests created by @dependabot with `do NOT merge` label
are used to test against higher library versions when they are released.

> `TestApplication.AspNet` is an exception to this strategy
> as it would not work well, because of multiple dependent packages.
> `TestApplication.AspNet` references the latest versions
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
  ~/repos/opentelemetry-dotnet-instrumentation$ source dev/envvars.sh 
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
