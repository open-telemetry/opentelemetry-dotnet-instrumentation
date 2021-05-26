# PoC - Reuse OpenTelemetry .NET API and SDK

## Vision

Use [OpenTelemetry .NET API and SDK](https://github.com/open-telemetry/opentelemetry-dotnet)
instead Datadog tracer model.

## Use Case

Instrumented HTTP client and HTTP server applciations exporting traces to Jaeger.
We may also try adding manual instrumentation.

## Constraints

We may add additional requirements for the instrumented applications such us:

- Add references to OpenTelemetry NuGet packages.
- Concrete versions of OpenTelemetry NuGet packages must be used.
- Must use newer versions of .NET.

## Plan

### Preparation

1. Prepare the use case secenario and make sure it is working.
1. Get rid of everything, but HTTP Client and HTTP Server instrumentation.
   Reason: quicker iterations (especially thanks to quicker build time).
   **Make sure the use case is still working**.

It would be good to have some automation here to test without doing to much stuff manually.

### Remove Datadog Tracer

1. Remove `Datadog.Trace.Tracer` and a lot of related code.
   We can just keep the AutoInstrumentation integration boilerplate for HTTP Client and ASP.NET.
1. Change the `Datadog.Trace.ClrProfiler.Instrumentation.Initialize()` method
   so that it will be the place where the global OTel trace provider will be configured. 

It would be good to have a checkpoint here where the instrumented application is NOOP-instrumented.

## Experiments

1. Hardcode the global OTel tracer configuration.
1. Configure the OTel tracer based on env vars. SDK should handle it.
   [question](https://cloud-native.slack.com/archives/C01N3BC2W7Q/p1620994235161800)
1. Add manual instrumentation to the applications.
1. Try auto-instrumentation using ActivitySource
   e.g. in `Datadog.Trace.ClrProfiler.AutoInstrumentation.Http.HttpClient.HttpMessageHandlerCommon`.
1. Test with different versions of OTel SDK in runtime.
1. Try using `AppDomain.CurrentDomain.AssemblyResolve +=` to add a fallback 
   if the instrumented app does not reference the OTel SDK.

## Helpful docs and samples

- https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/README.md#activity-source
- https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/examples/Console/TestHttpClient.cs

## Findings

### Required reference to System.Diagnostics.DiagnosticSource

The instrumented application has to include:

```xml
<PackageReference Include="System.Diagnostics.DiagnosticSource" Version="5.0.1" />
```

## Testing

### Additional setup for Windows

Add `msbuild` to your `PATH`. You can do it by adding to `~/.bashrc` something more or less like bellow:

```sh
PATH="$PATH:/c/Program Files (x86)/Microsoft Visual Studio/2019/Professional/MSBuild/Current/Bin"
```

### Additional setup for Linux and MacOS

```sh
sudo mkdir -p /var/log/opentelemetry/dotnet
sudo chmod a+rwx /var/log/opentelemetry/dotnet
```

### Usage

For .NET Core 3.1 run:

```sh
./poc.sh
```

For .NET 5.0 run:

```sh
aspNetAppTargetFramework=net5.0 consoleAppTargetFramework=net5.0 ./poc.sh
```

For .NET Framework run:

```sh
consoleAppTargetFramework=net46 ./poc.sh
```