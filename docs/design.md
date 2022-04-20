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

- **Zero-touch source code instrumentation**: Users can instrument applications
without changing the source. Build changes may be required through the addition
of specific NuGet packages.
- **Custom SDK support**: The instrumentation can initialize
the OpenTelemetry .NET SDK, though what OpenTelemetry SDK implementation is used
and its initialization can also be delegated to the application code.

### Unsupported scenarios

- **Applications using Ahead-of-Time (AOT) compilation**:
The current implementation relies on the [CLR Profiler APIs](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/)
and doesn't support AOT.
- **Side-by-side usage with other CLR Profiler based tools**: Various tools for .NET
are also implemented using a CLR Profiler. However, only a single CLR Profiler
can be used when running the application.

## Error handling

Initialization errors, usually caused by invalid configuration,
are logged and crash the application.

Errors occurring at application runtime are logged and should never crash the application.

## Architecture

To instrument a .NET application without requiring source code changes,
the OpenTelemetry .NET Instrumentation uses the [CLR Profiler APIs](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/)
to bootstrap the [OpenTelemetry .NET SDK](https://github.com/open-telemetry/opentelemetry-dotnet#readme)
and inject the selected instrumentations into the targeted application.

The main components of the project are:

- [**CLR Profiler DLL**](../src/OpenTelemetry.AutoInstrumentation.Native):
Native component that implements a CLR Profiler. The CLR Profiler is used to
modify the application [intermediate language](https://en.wikipedia.org/wiki/Common_Intermediate_Language)
 (IL), including the IL of packages used by the application, to add and collect
 observability data.

- [**Loader**](../src/OpenTelemetry.AutoInstrumentation.Loader):
Managed library shipped as a resource of the native CLR Profiler.
It loads the bootstrap code into the targeted application and extends the assembly
load paths to include the folders with the OpenTelemetry .NET SDK
and the instrumentations to be injected into the application.

- [**Managed Profiler**](../src/OpenTelemetry.AutoInstrumentation):
Contains the code to set up the OpenTelemetry .NET SDK and configured instrumentations,
as well as support code to run and implement bytecode instrumentations. Set the
`OTEL_DOTNET_AUTO_LOAD_AT_STARTUP` environment variable to `false` when the
application initializes the OpenTelemetry .NET SDK Tracer on its own.

- **Source Instrumentations**: Instrumentations created on top of API hooks
or callbacks provided directly by the library or framework being instrumented.
This type of instrumentation depends on the OpenTelemetry API and the specific
library or framework that they instrument. Some examples include:

  - [ASP.NET Core Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.AspNetCore)
  - [gRPC Client Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.GrpcNetClient)
  - [HttpClient and HttpWebRequest Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.Http)

- **Bytecode Instrumentations**: Instrumentations created for libraries
or frameworks that lack proper hooks or callbacks to allow the collection
of observability data. These instrumentations must be implemented following
the proper attribute annotation so that the native CLR Profiler implementation
can inject them at runtime. Some examples include:

  - [GraphQL](../src/OpenTelemetry.AutoInstrumentation/Instrumentations/GraphQL)

![Overview](./images/architecture-overview.png)

### Injecting the OpenTelemetry .NET SDK and instrumentations

The OpenTelemetry .NET SDK and selected source instrumentations are injected
into the target process through a series of steps started by the CLR Profiler DLL:

1. On the [CorProfiler::ModuleLoadFinished](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/icorprofilercallback-moduleloadfinished-method)
callback, the CLR Profiler DLL takes the following actions:

   - If the loaded module is in the set of modules for which there is bytecode instrumentation,
   or if it's the first non-corelib module or not in one of the special cases,
   the profiler adds the module to a map of modules to be instrumented.
   - If there is bytecode instrumentation for the module, the profiler requests
   a JIT recompilation using
   [ICorProfilerInfo::RequestReJIT](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/icorprofilerinfo4-requestrejit-method)
   for the methods targeted by the bytecode instrumentation.

2. On the [CorProfiler::JITCompilationStarted](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/icorprofilercallback-jitcompilationstarted-method)
callback, the CLR Profiler DLL takes the following actions:

   - If instrumenting he first module in the current AppDomain,
   the profiler injects the IL calling the Loader `Startup` type constructor.
   This type of constructor:

      - Adds an event handler to the
      [`AssemblyResolve`](https://docs.microsoft.com/en-us/dotnet/api/system.appdomain.assemblyresolve?view=net-5.0),
      so that it can add any assembly needed by the SDK itself or by any instrumentation.
      - Runs, through reflection, the `Initialization` method from
        the Managed Profiler assembly.
        - The `Initialization` code bootstraps the OpenTelemetry .NET SDK,
          adding configured processors, exporters, and so on,
          and initializes any configured source instrumentations.
   - If the first method observed by JITCompilationStarted is IIS startup code,
     the profiler invokes
     `AppDomain.CurrentDomain.SetData("OpenTelemetry_IISPreInitStart", true)`,
     so that automatic instrumentation
    correctly handles IIS startup scenarios.

### Bytecode instrumentations

The bytecode instrumentation, called "call target" in this repo, relies on
the JIT recompilation capability of the CLR to rewrite the IL for instrumented
methods. This adds logic at the beginning and end of the instrumented methods
to invoke instrumentation written in this repo, and wraps the calls with
try-catch blocks to prevent instrumentation errors from affecting the normal operation
of the application.

Bytecode instrumentation methods should not have direct dependencies with
the libraries that they instrument. This way, they can work with multiple
versions of the assemblies targeted for instrumentation and reduce the number
of shipped files.

When operating with parameters and return values of the targeted methods,
the instrumentation methods must use [DuckTyping](../src/OpenTelemetry.AutoInstrumentation.Managed/DuckTyping/README.md)
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

#### Configuration resolution

.NET Core [Framework-dependent deployment](https://docs.microsoft.com/en-us/dotnet/core/deploying/deploy-with-cli#framework-dependent-deployment)
applications might use [DOTNET_ADDITIONAL_DEPS](https://github.com/dotnet/runtime/blob/main/docs/design/features/additional-deps.md)
and [DOTNET_SHARED_STORE](https://docs.microsoft.com/en-us/dotnet/core/deploying/runtime-store)
from OpenTelemetry .NET Automatic Instrumentation installation location
to resolve assembly conflicts.

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

## Further reading

OpenTelemetry:

- [OpenTelemetry website](https://opentelemetry.io/)
- [OpenTelemetry Specification](https://github.com/open-telemetry/opentelemetry-specification)

Microsoft .NET Profiling APIs:

- [Profiling API](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/)
- [Metadata API](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/metadata/)
- [The Book of the Runtime - Profiling](https://github.com/dotnet/coreclr/blob/master/Documentation/botr/profiling.md)
