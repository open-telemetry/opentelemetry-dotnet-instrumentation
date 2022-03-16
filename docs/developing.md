# Development

## Development Environment

On all platforms, the minimum requirements are:

- Git client and command line tools
- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- [.NET Core 3.1 Runtime](https://dotnet.microsoft.com/download/dotnet-core/3.1)

### Windows

- [Visual Studio 2022 (17.1)](https://visualstudio.microsoft.com/downloads/) or newer
  - Workloads (with recommeded components):
    - ASP.NET and web development
    - .NET desktop development
    - Desktop development with C++
  - Individual components:
    - MSVC v142 - VS 2019 C++ x64/x86 build tools (v14.29)
- [Docker for Windows](https://docs.docker.com/docker-for-windows/)

Microsoft provides
[evaluation developer VMs](https://developer.microsoft.com/en-us/windows/downloads/virtual-machines)
with Windows and Visual Studio pre-installed.

### Linux and MacOS

- cmake, make, gcc, clang, clang++
- [Docker](https://docs.docker.com/engine/install/)
- [Docker Compose](https://docs.docker.com/compose/install/)

When using [C# for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp),
because of [Mono missing features](https://github.com/OmniSharp/omnisharp-vscode#note-about-using-net-6-sdks),
`omnisharp.useGlobalMono` has to be set to `never`.
Go to `File` -> `Preferences` -> `Settings` -> `Extensions` -> `C# Configuration` ->
Change `Omnisharp: Use Global Mono` (you can search for it if the menu is too long) to `never`.
Afterwards, you have restart OmniSharp: `F1` -> `OmniSharp: Restart OmniSharp`.

There may be a lot of errors, because some projects target .NET Framework.
You can switch try `F1` -> `OmniSharp: Select Project` in Visual Studio Code
to load a subset of projects which work without any issues.
You can also try building the projects which have errors.

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

To build you can simply run:

```cmd
dotnet nuke
```

The main build artifacts are located under `bin/tracer-home`.

Clean your repository by running:

```cmd
git clean -fXd
```

## Manual Testing

### Test Environment

The [`dev/docker-compose.yaml`](../dev/docker-compose.yaml) contains
configuration for running OTel Collector and Jaeger.

You can run the services using:

```sh
docker-compose -f dev/docker-compose.yaml up
```

The following Web UI endpoints are exposed:

- <http://localhost:16686/search> - collected traces,
- <http://localhost:8889/metrics> - collected metrics,
- <http://localhost:13133> - collector's health.

### Instrument an application

> *Caution:* Make sure to build and prepare the test environment beforehand.

[`dev/instrument.sh`](../dev/instrument.sh) helps to run a command
with auto-instrumentation in your shell (e.g. bash, zsh, git bash).

Example usage:

```sh
./dev/instrument.sh OTEL_DOTNET_AUTO_ENABLED_INSTRUMENTATIONS=HttpClient dotnet run -f netcoreapp3.1 --project ./samples/ConsoleApp/ConsoleApp.csproj
```

[`dev/envvars.sh`](../dev/envvars.sh) can be used to export profiler environmental variables to your current shell session.
**It has to be executed from the root of this repository**. Example usage:

```sh
source ./dev/envvars.sh
export OTEL_DOTNET_AUTO_ENABLED_INSTRUMENTATIONS=HttpClient
./samples/ConsoleApp/bin/Debug/netcoreapp3.1/ConsoleApp
```

### Run Samples

The sample applications can be used to see the local changes in action, see the
[`samples/README.md`](../samples/README.md) for instructions on how to run the samples.

## Debug .NET Runtime on Linux

- [Requirements](https://github.com/dotnet/runtime/blob/main/docs/workflow/requirements/linux-requirements.md)

- [Building .NET Runtime](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/libraries/README.md)

  ```bash
  ./build.sh clr+libs
  ```

- [Using `corerun`](https://github.com/dotnet/runtime/blob/main/docs/workflow/testing/using-corerun.md)

  ```bash
  PATH="$PATH:$PWD/artifacts/bin/coreclr/Linux.x64.Debug/corerun"
  export CORE_LIBRARIES="$PWD/artifacts/bin/runtime/net6.0-Linux-Debug-x64"
  corerun ~/repos/opentelemetry-dotnet-instrumentation/samples/ConsoleApp/bin/Debug/net6.0/ConsoleApp.dll
  ```

- [Debugging](https://github.com/dotnet/runtime/blob/main/docs/workflow/debugging/coreclr/debugging.md)

  Example showing how you can debug if the profiler is attached properly:

  ```bash
  ~/repos/opentelemetry-dotnet-instrumentation$ source dev/envvars.sh 
  ~/repos/opentelemetry-dotnet-instrumentation$ cd ../runtime/
  ~/repos/runtime$ lldb -- ./artifacts/bin/coreclr/Linux.x64.Debug/corerun ~/repos/opentelemetry-dotnet-instrumentation/samples/ConsoleApp/bin/Debug/net6.0/ConsoleApp.dll
  (lldb) target create "./artifacts/bin/coreclr/Linux.x64.Debug/corerun"
  Current executable set to '/home/user/repos/runtime/artifacts/bin/coreclr/Linux.x64.Debug/corerun' (x86_64).
  (lldb) settings set -- target.run-args  "/home/user/repos/opentelemetry-dotnet-instrumentation/samples/ConsoleApp/bin/Debug/net6.0/ConsoleApp.dll"
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

  You may need to add a [`dlerror()`](https://linux.die.net/man/3/dlerror) call
  in order to get the error message. Example:

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
