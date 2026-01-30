# Design

## Vision

The following are the goals that define the long-term vision for the project.
The vision guides daily activities, design, and feature acceptance.

- **High performance**: Automatic instrumentation performance impact
should not be a concern for users.
- **Reliability**: Stable and performant under different loads. Well-behaved
under extreme load, with low, predictable resource consumption.
- **Visibility**: Users should be able to generate telemetry that provides deep
and detailed visibility into their applications. Such telemetry must allow users
to identify and solve application-related issues in production.
- **Useful by default**: After installations users should be able to get telemetry
from targeted libraries with none or minimal configuration,
thanks to a good selection of default settings.
- **Extensible**: Users can choose key components through configuration and plugins.

## Supported and unsupported scenarios

### Supported scenarios

- **Automatic instrumentation**: Users can instrument applications
without changing the source code. Build changes may be required through the addition
of specific NuGet packages.
- **Custom SDK support**: The instrumentation can initialize
the OpenTelemetry .NET SDK, though what OpenTelemetry SDK implementation is used
and its initialization can also be delegated to the application code.

### Unsupported scenarios

- **Applications using Ahead-of-Time (AOT) compilation**:
The current implementation relies on the [host startup hook](https://github.com/dotnet/runtime/blob/main/docs/design/features/host-startup-hook.md)
or the [CLR Profiler API](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling)
and neither is supported when publishing AOT compiled applications.
- **Side-by-side usage with other CLR Profiler based tools**: Various tools for
.NET are also implemented using a CLR Profiler. However, only a single CLR Profiler
can be used when running the application. The CLR Profiler component is required
on the **.NET Framework** and optional for **.NET** applications, more info below.

## Error handling

Initialization errors, usually caused by invalid configuration,
are logged. If possible, default configuration is used otherwise it crash the application.

Errors occurring at application runtime are logged and should never crash the application.

## Automatic instrumentation workflow

To instrument a .NET application without source code changes, do the following:

 1. Inject the [OpenTelemetry .NET SDK](https://github.com/open-telemetry/opentelemetry-dotnet#readme)
 into the application.
 2. Add and enable instrumentations as the targeted libraries are loaded
 into the application.

### Injecting the OpenTelemetry .NET SDK

#### **.NET** applications

The [OpenTelemetry .NET SDK](https://github.com/open-telemetry/opentelemetry-dotnet#readme)
is injected using the [host startup hook](https://github.com/dotnet/runtime/blob/main/docs/design/features/host-startup-hook.md).
This allows the OpenTelemetry .NET SDK to be configured before any application code
runs. Although the OpenTelemetry .NET SDK is injected into a .NET application
without using a CLR Profiler,
the latter is still required to enable bytecode instrumentations. See the next
section for more information.

#### **.NET Framework** applications

.NET Framework doesn't support the host startup hook. The OpenTelemetry .NET SDK
is injected using the [CLR Profiler APIs](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/),
which allow modifying application code during execution.

### Library instrumentations

There are two broad instrumentations types injected into the applications:

- **Source instrumentations**: instrumentations created on top of API hooks
or callbacks provided directly by the library or framework being instrumented.
This type of instrumentation depends on the OpenTelemetry API and the specific
library or framework that they instrument. Some examples include:

  - [ASP.NET Core Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.AspNetCore)
  - [gRPC Client Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.GrpcNetClient)
  - [HttpClient and HttpWebRequest Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.Http)

- **Bytecode instrumentations**: instrumentations created for libraries
or frameworks that lack proper hooks or callbacks to allow the collection
of observability data. These instrumentations are enabled by modifying
the application [IL code](https://en.wikipedia.org/wiki/Common_Intermediate_Language)
during runtime using the [CLR Profiler API](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/).
Bytecode instrumentations must be implemented following
the proper attribute annotation so that the native CLR Profiler implementation
can inject them at runtime. Some examples include:

  - [Logger](../src/OpenTelemetry.AutoInstrumentation/Instrumentations/Logger/)
  - [MongoDB](../src/OpenTelemetry.AutoInstrumentation/Instrumentations/MongoDB/)

Both kinds of instrumentation are enabled only when the targeted modules are loaded
into the targeted application.

## Architecture

The main components of the project are:

- [**Loader**](../src/OpenTelemetry.AutoInstrumentation.Loader):
Managed library that bootstraps the **Managed Profiler** code into the
targeted application, extending the
load paths to include the folders with the OpenTelemetry .NET SDK
and the instrumentations to be injected into the application.

- [**Managed Profiler**](../src/OpenTelemetry.AutoInstrumentation):
Contains the code to set up the OpenTelemetry .NET SDK and configured instrumentations,
as well as support code to run and implement bytecode instrumentations.

- [**CLR Profiler DLL**](../src/OpenTelemetry.AutoInstrumentation.Native):
Native component that implements a CLR Profiler. The CLR Profiler is used to
modify the application [intermediate language](https://en.wikipedia.org/wiki/Common_Intermediate_Language)
(IL), including the IL of packages used by the application to add and collect
observability data. On the .NET Framework the CLR Profiler DLL also injects
the **Loader** (see above) during the application startup.

![Overview](./images/architecture-overview.png)

### Bootstrapping

The initial mechanism for bootstrapping the OpenTelemetry .NET SDK differs
between .NET and .NET Framework. As the [host startup hook](https://github.com/dotnet/runtime/blob/main/docs/design/features/host-startup-hook.md)
is not available for .NET Framework, the initialization is done in both cases by
creating one instance of the `OpenTelemetry.AutoInstrumentation.Loader.Startup`
class from the Loader assembly. When creating the instance, the static constructor
of the type performs the following actions:

1. Adds a handler to the [`AssemblyResolve` event](https://docs.microsoft.com/en-us/dotnet/api/system.appdomain.assemblyresolve?view=net-5.0),
so that it can add any assembly needed by the SDK itself or by any instrumentation.
2. Runs, through reflection, the `Initialization` method from
  the `OpenTelemetry.AutoInstrumentation.Instrumentation` type
  from the Managed Profiler assembly.
  a. The `Initialization` code bootstraps the OpenTelemetry .NET SDK,
    adding configured processors, exporters, and so on,
    and setting the mechanisms to enable any configured source instrumentations.

#### .NET bootstrapping

.NET applications rely on the [host startup hook](https://github.com/dotnet/runtime/blob/main/docs/design/features/host-startup-hook.md)
being configured to use the `StartupHook.Initialize()` method from
the `OpenTelemetry.AutoInstrumentation.StartupHook` assembly.
The `Initialize` method loads the `OpenTelemetry.AutoInstrumentation.Loader`
assembly and creates the `OpenTelemetry.AutoInstrumentation.Loader.Startup` instance.

#### .NET Framework bootstrapping

.NET Framework bootstrapping is performed on the [CorProfiler::JITCompilationStarted](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/icorprofilercallback-jitcompilationstarted-method)
callback. When that callback happens the CLR Profiler DLL takes
the following actions:

1. If the module is the first user module in the current AppDomain,
  the profiler injects the IL to call the Loader `Startup` constructor.
2. If the first method observed by `JITCompilationStarted` is IIS startup code,
  the profiler invokes
  `AppDomain.CurrentDomain.SetData("OpenTelemetry_IISPreInitStart", true)`,
  so that automatic instrumentations can correctly handle IIS startup scenarios.

### Injecting instrumentations

#### Source instrumentation

Source instrumentations are injected into the target process by adding
handlers to the [`AssemblyLoad` event](https://learn.microsoft.com/en-us/dotnet/api/system.appdomain.assemblyload?view=net-7.0)
and when the targeted assembly loads and triggers the source instrumentation
initialization code.

#### Bytecode instrumentation

Bytecode instrumentations rely on
the JIT recompilation capability of the CLR to rewrite the IL for instrumented
methods. This adds logic at the beginning and end of the instrumented methods
to invoke instrumentation included in this project, and wraps the calls with
try-catch blocks to prevent instrumentation errors from affecting the normal operation
of the application. This IL code rewrite happens in the following steps:

1. On the [CorProfiler::ModuleLoadFinished](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/icorprofilercallback-moduleloadfinished-method)
callback, the CLR Profiler DLL takes the following actions:

   - If the loaded module is in the set of modules for which there is bytecode instrumentation,
   the profiler adds the module to a map of modules to be instrumented.
   - The profiler then requests
   a Re-JIT recompilation using
   [ICorProfilerInfo::RequestReJIT](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/icorprofilerinfo4-requestrejit-method)
   for the methods targeted by the bytecode instrumentation.

2. On the ReJIT callback the CLR Profiler DLL finds the corresponding
instrumentation code and wraps it around the target code.

Bytecode instrumentation methods should not have direct dependencies with
the libraries that they instrument. This way, they can work with multiple
versions of the assemblies targeted for instrumentation and reduce the number
of shipped files.

When operating with parameters and return values of the targeted methods,
the instrumentation methods must use [DuckTyping](../src/OpenTelemetry.AutoInstrumentation/DuckTyping/README.md)
or [reflection](https://docs.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/reflection)
to access objects from the APIs being instrumented.

### Assembly conflict resolution

The injection of the OpenTelemetry .NET SDK and any source instrumentation brings
the risk of assembly version conflicts. This issue is more likely with the
[NuGet package System.Diagnostic.DiagnosticSource](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/)
and its dependencies, because it contains the [Activity type](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity?view=net-5.0)
used by the OpenTelemetry .NET API to represent a span. This package, previously
released by Microsoft, is already used by various applications.

Two issues might arise from incorrect versioning:

1. Version required by the OpenTelemetry .NET SDK or the instrumentations
   is not met.
2. Multiple versions of the assembly in the same process,
   as the runtime treats them independently.

On the .NET Framework and .NET, by default, the CLR Profiler redirects any
assembly references to the versions shipped with the Managed Profiler.

#### Build time resolution

Currently, the path to resolving such conflicts is to add or update any package
reference used by the application to the versions required by
the OpenTelemetry .NET SDK and the instrumentations.
Even if the application itself doesn't directly reference a conflicting
dependency, this might still be necessary due to conflicts created by
any indirect dependency.

Adding or updating package references works because of the way
[NuGet Package Dependency Resolution](https://docs.microsoft.com/en-us/nuget/concepts/dependency-resolution)
is implemented. Conflicts are resolved by having explicit package references
to the correct package versions.

To simplify this process, we plan to create a NuGet package that installs
the CLR Profiler and its managed dependencies.

#### Runtime time resolution

If you can't change the application build to add or update the necessary package
versions, you can still address conflicts using the methods described in
[Handling of Assembly version Conflicts](./troubleshooting.md#handling-of-assembly-version-conflicts).

### `System.Diagnostics.DiagnosticSource` versions

The version of `System.Diagnostics.DiagnosticSource` used by the instrumentation
differs depending on the .NET version:

- .NET Framework is referencing the latest supported version.
  Automatic redirection is forcing this version
  if instrumented application bring own copy.
- .NET is referencing the latest supported version for a given runtime (e.g. for net8.0 latest 8.x, net9.0 - latest 9.x etc.).
  The version can be upgraded by the instrumented application.

## Further reading

OpenTelemetry:

- [OpenTelemetry website](https://opentelemetry.io/)
- [OpenTelemetry Specification](https://github.com/open-telemetry/opentelemetry-specification)

Microsoft .NET Profiling APIs:

- [Profiling API](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/)
- [Metadata API](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/metadata/)
- [The Book of the Runtime - Profiling](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/profiling.md)
