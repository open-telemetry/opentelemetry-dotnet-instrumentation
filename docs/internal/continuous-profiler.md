# Continuous profiler

> [!IMPORTANT]  
> Continuous profiler is an experimental feature. It will be subject to change,
> when <https://github.com/open-telemetry/oteps/pull/239> or <https://github.com/open-telemetry/oteps/pull/237>
> are merged.

The continuous profiler collects stack traces from the processes for two type of
events:

* Periodically, for all threads. See [Thread sampling](#thread-sampling).
* Memory allocation events. See [Allocation sampling](#allocation-sampling).

You can export stack traces to any observability back end that supports profiling.

## Thread sampling

You can enable thread sampling using the custom plugin, which
can parse dense thread sampling data and export it.

### How does the thread sampler work?

The profiler uses the
[.NET profiler](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/)
to perform periodic call stack sampling. For every sampling period, the runtime
suspends execution and the samples for all managed thread are saved in the buffer,
then the runtime resumes.

The separate managed thread processes data from the buffer and exports it
in the format defined by the plugin. To make the process more efficient, the
sampler uses two independent buffers to store samples alternatively.

### Requirements

* .NET 6.0 or higher, OR
* .NET Framework 4.6.2 or higher (Windows x64 only)

### .NET Framework support

Thread sampling is supported on .NET Framework 4.6.2+ running on Windows x64.
No additional configuration is required beyond implementing and configuring
the custom plugin in exactly the same manner as you would for .NET (Core).
Behind the scenes, the samples are captured and exported in the same format
as they would be in .NET.

> [!NOTE]  
> .NET Framework support uses a different native stack walking strategy
> optimized for the .NET Framework runtime, but the exported data format
> remains identical to .NET (Core).

### Enable the profiler

Implement custom plugin. See plugin section.

### Configuration defaults

* `threadSamplingEnabled = true;`: Enables thread sampling.
* `var threadSamplingInterval = 10000u;`: Sampling interval, in milliseconds.
  Lowest recommended value is 1000.
* `var exportInterval = TimeSpan.FromMilliseconds(500);`: Interval for reading
  the data from buffers and call the exporter. This setting is common for both
  thread and allocation sampling.
* `object continuousProfilerExporter = new ConsoleExporter();`: Exporter to be
  used for both thread and allocation sampling.

### Escape hatch

The profiler limits its own behavior when both buffers used to store sampled
data are full.

This scenario might happen when the data processing thread is not able
to export data the given period of time.

Thread sampling resumes when any of the buffers are empty.

### Troubleshoot the .NET profiler

#### How do I know if it's working?

At startup, the OpenTelemetry Instrumentation for .NET logs the string
`ContinuousProfiler::StartThreadSampling` at `info` log level.

You can grep for this in the native logs for the instrumentation
to see something like this:

```text
10/12/22 12:10:31.962 PM [12096|22036] [info] ContinuousProfiler::StartThreadSampling
```

#### How can I see Continuous Profiling configuration?

The OpenTelemetry .NET Automatic Instrumentation logs the profiling configuration
at `Debug` log level during the startup. You can grep for the string
`Continuous profiling configuration:` to see the configuration.

#### What does the escape hatch do?

The escape hatch automatically discards profiling data
if the ingest limit has been reached.

If the escape hatch activates, it logs the following message:

```text
Skipping a thread sample period, buffers are full.
```

You can also look for:

```text
** THIS WILL RESULT IN LOSS OF PROFILING DATA **.
```

If you see these log messages, check the exporter implementation.

#### What if I'm on an unsupported .NET version?

For thread sampling, you need either:

* .NET 6.0 or higher, OR
* .NET Framework 4.6.2 or higher (Windows x64 only)

If you're on an unsupported version (e.g., .NET Core 3.1, .NET 5.0, or
.NET Framework on non-x64 platforms), you'll need to upgrade to a supported
runtime version.

#### Can I tell the sampler to ignore some threads?

There is no such functionality. All managed threads are captured by the profiler.

### Troubleshoot .NET Framework thread sampling

This section covers troubleshooting specific to .NET Framework thread sampling.
Enable `debug` level logging in the native profiler to see detailed stack
capture diagnostics.

#### How do I know if .NET Framework stack capture is initialized?

Look for the following message in the native logs:

```text
[debug] [StackCapture] Canary thread ready
```

This indicates successful initialization of the stack capture machinery.
The canary thread is used internally to verify that stack walking operations
are safe to perform.

#### How do I know if stack capture is working?

Look for the following message in the native logs:

```text
[debug] [StackCapture] Unseeded capture succeeded.
```

This indicates that stack samples are being successfully captured for threads.

#### What does "Unable to locate managed frame" mean?

You may see messages like:

```text
[debug] [StackCapture] PrepareContextForSnapshot - Unable to locate managed frame in stack walk for ThreadID=...
```

**This is normal and expected behavior.** This message appears when a thread is
executing code that cannot be resolved to a managed frame, such as:

* Native code execution
* System calls
* Transitions between managed and native code
* Threads blocked in native wait states

The profiler will continue to capture samples from other threads and will
successfully capture this thread's stack in subsequent sampling intervals
when it returns to managed code.

#### What if I don't see the canary thread ready message?

If you don't see `[StackCapture] Canary thread ready` in the logs:

1. Verify that the application is running on Windows x64
2. Ensure thread sampling is enabled in the plugin configuration
3. Check that the profiler is successfully attached (look for
   `ContinuousProfiler::StartThreadSampling` in the logs)

## Allocation sampling

The profiler samples allocations, captures the call stack state for the .NET
thread that triggered the allocation, and exports it in the appropriate format.

Use the memory allocation data, together with the stack traces and .NET runtime
metrics, to investigate memory leaks and unusual consumption patterns
in an observability back end that supports profiling.

### How does the memory profiler work?

The profiler leverages [.NET profiling](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/)
to perform allocation sampling.
For every sampled allocation, allocation amount together with stack trace of
the thread that triggered the allocation, and associated span context, are saved
into buffer.

The managed thread shared with CPU Profiler processes the data from the buffer
and exports in the way defined by the plugin..

### Requirements

* .NET 6.0 or higher (`ICorProfilerInfo12` available in runtime)

> [!NOTE]  
> Allocation sampling is **not supported** on .NET Framework. The required
> `ICorProfilerInfo10` and `ICorProfilerInfo12` interfaces for allocation tick
> events are not available in the .NET Framework runtime.

### Enable the profiler

Implement custom plugin.

### Configuration settings by the plugin

```csharp
threadSamplingEnabled, threadSamplingInterval, allocationSamplingEnabled, maxMemorySamplesPerMinute, exportInterval, continuousProfilerExporter
```

* `allocationSamplingEnabled = true`
* `maxMemorySamplesPerMinute = 200` // minimum value: 1, Splunk is using 200 by
  default
* `exportInterval = TimeSpan.FromMilliseconds(500);` // Interval to read data from
  buffers and call exporter, common for Thread and Allocation sampling
* `object continuousProfilerExporter = new ConsoleExporter();` // Exporter, common
  for Thread and Allocation sampling

### Escape hatch

The profiler limits its own behavior when buffer
used to store allocation samples is full.

Current maximum size of the buffer is 200 KiB.

This scenario might happen when the data processing thread is not able
to export the data by the plugin in the given time frame.

### Troubleshooting the .NET profiler

#### How do I know if it's working?

At the startup, the OpenTelemetry .NET Automatic Instrumentation will log the string
`ContinuousProfiler::MemoryProfiling started` at `info` log level.

You can grep for this in the native logs for the instrumentation
to see something like this:

```text
10/12/23 12:10:31.962 PM [12096|22036] [info] ContinuousProfiler::MemoryProfiling started.
```

#### How can I see Continuous Profiling configuration?

The OpenTelemetry .NET AutomaticInstrumentation logs the profiling configuration
at `Debug` log level during the startup. You can grep for the string
`Continuous profiling configuration:` to see the configuration.

#### What does the escape hatch do?

The escape hatch automatically discards captured allocation data
if the ingest limit has been reached.

If the escape hatch activates, it logs the following message:

`Discarding captured allocation sample. Allocation buffer is full.`

If you see these log messages, check the configuration and communication layer
between your process and the Collector.

#### What if I'm on an unsupported .NET version?

Allocation sampling requires .NET 6.0 or higher. It is not available on
.NET Framework due to missing `ICorProfilerInfo10`/`ICorProfilerInfo12` APIs
for allocation tick events. You'll need to use .NET 6.0+ if you require
allocation sampling.

## Feature support matrix

| Feature             | .NET 6.0+    | .NET Framework 4.6.2+           |
| ------------------- | ------------ | ------------------------------- |
| Thread sampling     | ✅ Supported | ✅ Supported (Windows x64 only) |
| Allocation sampling | ✅ Supported | ❌ Not supported                |

## Plugin

For now, the plugins is responsible for

* defining configuration for continuous profiling
* providing exporter for the allocation and profiling data
* *parsing data* prepared by the native code.

### Plugin contract

> [!IMPORTANT]  
> It will be subject to change, when <https://github.com/open-telemetry/oteps/pull/239>
> or <https://github.com/open-telemetry/oteps/pull/237> will be ready and merged.

As other methods, `GetContinuousProfilerConfiguration` is called by reflection
and convention.

```csharp
/// <summary>
/// Configure Continuous Profiler.
/// </summary>
/// <returns>(threadSamplingEnabled, threadSamplingInterval, allocationSamplingEnabled, maxMemorySamplesPerMinute, exportInterval, exportTimeout, continuousProfilerExporter)</returns>
public Tuple<bool, uint, bool, uint, TimeSpan, TimeSpan, object> GetContinuousProfilerConfiguration()
{
    var threadSamplingEnabled = true; // enables thread sampling
    var threadSamplingInterval = 10000u; // interval to stop CLR runtime and fetch stacks. 10 000ms is Splunk default. 1000ms is the lowest supported value by Splunk. The code does not contains any limitations this. Plugins is responsible for checks.
    var allocationSamplingEnabled = true; // enables allocation sampling (ignored on .NET Framework)
    var maxMemorySamplesPerMinute = 200u; // max number of samples in minutes. 200 is tested default value by Splunk.
    var exportInterval = TimeSpan.FromMilliseconds(500); // Pause time before next execution of exporting/reading buffer  process
    var exportTimeout = TimeSpan.FromMilliseconds(500); // Export timeout
    object continuousProfilerExporter = new ConsoleExporter();
    return Tuple.Create(threadSamplingEnabled, threadSamplingInterval, allocationSamplingEnabled, maxMemorySamplesPerMinute, exportInterval, exportTimeout, continuousProfilerExporter);
}
```

if more than one plugin implement `GetContinuousProfilerConfiguration` only
the first one will be used. Other will be ignored.

> [!NOTE]  
> On .NET Framework, the `allocationSamplingEnabled` setting is ignored since
> allocation sampling is not supported. The same plugin configuration works
> for both .NET and .NET Framework - thread sampling will be enabled on both
> platforms when configured.

### Exporter contract

Two methods has to be implemented by Exporter

```csharp
public void ExportThreadSamples(byte[] buffer, int read, CancellationToken cancellationToken);
public void ExportAllocationSamples(byte[] buffer, int read, CancellationToken cancellationToken);
```

Both accept buffer produced by the native code, the length of filled
data, and cancellation token.
The Exporter is responsible both for parsing this buffer and exporting it.

Example: [`OtlpOverHttpExporter`](../../test/test-applications/integrations/TestApplication.ContinuousProfiler/Exporter/OtlpOverHttpExporter.cs).

> [!NOTE]  
> The `ExportAllocationSamples` method will not be called on .NET Framework
> since allocation sampling is not supported. However, the exporter must still
> implement this method to satisfy the contract.

### Native parser

As there is no default OpenTelemetry Protocol format there is not easy way to
create good contract between OpenTelemetry Automatic Instrumentation and
the plugin. The plugin has to implement (copy) our version of the parser.
It should be changed when the OTel Proposal will be merged, and we can start implementing
real OTLP exporter.

Implementation can be found in [`SampleNativeFormatParser`](../../test/test-applications/integrations/TestApplication.ContinuousProfiler/Exporter/SampleNativeFormatParser.cs).

The same parser implementation works for both .NET and .NET Framework - the
native buffer format is identical across both platforms.
