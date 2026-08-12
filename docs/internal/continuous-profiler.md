# Continuous profiler

> [!IMPORTANT]  
> Continuous profiler is an experimental feature. It will be subject to change,
> when <https://github.com/open-telemetry/oteps/pull/239> or <https://github.com/open-telemetry/oteps/pull/237>
> are merged.
> When this doc refers to .NET Framework supportability, it refers
> to Windows x86 and x64; ARM64 is not supported by
> OpenTelemetry .NET Automatic Instrumentation.

The continuous profiler collects stack traces from the processes for two types of
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
suspends execution and the samples for all managed threads are saved in the buffer,
then the runtime resumes.

A separate managed thread processes data from the buffer and exports it
in the format defined by the plugin. Completed sample batches are placed in a
first-in, first-out queue that holds at most two batches.

### Requirements

* .NET 8.0 or higher, OR
* .NET Framework 4.6.2 or higher

### .NET Framework support

Thread sampling works on .NET Framework 4.6.2+ (Windows) and requires no extra
plugin configuration beyond the normal plugin contract. Allocation sampling is
not supported on .NET Framework. Internally the native stack-walking strategy
differs on .NET Framework, but exported data format and plugin contract remain
the same.

### Enable the profiler

Implement custom plugin. See plugin section.

### Initial configuration

The default configuration disables thread and allocation sampling. Sampling and
export intervals are zero, and no exporter is configured.

To enable thread sampling, implement a
[continuous profiling plugin](../plugins.md#continuous-profiling) and provide a
positive sampling interval, positive export interval and timeout, and an
exporter. The lowest recommended sampling interval is 1000 milliseconds.

### Runtime reconfiguration

Native continuous profiler resources are initialized once per process through
`ConfigureContinuousProfilerV2`. The original `ConfigureContinuousProfiler`
entrypoint remains an ABI-compatible wrapper for older managed runtimes. The
managed batch reader similarly uses `ContinuousProfilerReadThreadSamplesV2`;
the original two-argument `ContinuousProfilerReadThreadSamples` entrypoint is
retained for older managed runtimes. Runtime setters do not initialize native
resources or managed export pipelines. After initialization, the six setters are:

* `SetContinuousProfilerEnabled(bool)` and
  `SetContinuousProfilerSamplingInterval(uint)` for CPU sampling.
* `SetContinuousProfilerAllocationSamplingEnabled(bool)` and
  `SetContinuousProfilerMaxMemorySamplesPerMinute(uint)` for allocation sampling.
* `SetContinuousProfilerSnapshotsEnabled(bool)` and
  `SetContinuousProfilerSnapshotSamplingInterval(uint)` for snapshot (selective)
  sampling.

Value setters retain the configured value while the corresponding service is
disabled. An enable setter reuses that value. The shared CPU/snapshot sampling
thread observes interval changes at cycle boundaries; a cycle already waiting or
in progress may use the previous interval. Configuration changes do not wake the
stop condition variable. Stopping the final service does wake the thread
immediately.

The corresponding managed interop methods use the
`SetNativeContinuousProfiler...` prefix. They return `true` when the requested
native operation succeeds and `false` when it cannot be applied. Successful
idempotent start and stop operations also return `true`. The managed exporter is
started only by the `AppDomain` that successfully owns the one-time native
initialization. A failed initial configuration does not start an exporter and
the owner stops any sampling service that was partially configured.

To enable thread sampling later when it is initially disabled, the initial
managed configuration must prepare the thread-sampling export pipeline with an
exporter, a non-zero sampling interval, and positive export interval and timeout.
Runtime operations cannot initialize this pipeline.

The same preparation rule applies to allocation sampling. To enable it later,
provide an exporter, a non-zero `MaxMemorySamplesPerMinute`, and positive export
interval and timeout in the initial managed configuration. The allocation
setters can then enable or disable sampling and update its maximum sample rate.
Snapshots can be disabled, re-enabled, or updated only when selective sampling
was prepared during initial configuration. Set
`SelectiveSamplerConfiguration.Enabled` to `false` to prepare that pipeline
without starting snapshot sampling. Managed initialization explicitly signals
native code only after the corresponding export handler is registered; runtime
calls cannot promote an unprepared pipeline.

Start and stop operations are idempotent and use at most one native sampling
thread. The thread is shared with selective sampling, so disabling continuous
thread sampling does not stop it while selective sampling remains enabled.
Runtime interval changes must satisfy the [selective sampling interval
constraints](./selective-sampling.md#limits).

These operations are internal managed/native interop and are not part of the
`IContinuousProfilerPlugin` contract.

### Escape hatch

The profiler limits its own behavior when both slots in the completed-batch
queue are full.

This scenario might happen when the data processing thread is not able
to export data the given period of time.

Thread sampling resumes when a slot becomes available.

### Troubleshoot the .NET profiler

#### How do I know if it's working?

When the shared native sampling thread starts or stops, the OpenTelemetry
Instrumentation for .NET logs these strings at `info` level:

```text
ContinuousProfiler sampling thread started.
ContinuousProfiler sampling thread stopped.
```

These messages describe the lifecycle of the thread shared by continuous and
selective sampling. A runtime CPU enable or disable operation may reuse that
thread and therefore emit neither message. Check the debug configuration log and
exported thread samples to verify continuous thread sampling.

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

#### Can I tell the sampler to ignore some threads?

There is no such functionality. All managed threads are captured by the profiler.

### Troubleshoot .NET Framework thread sampling

This section covers troubleshooting specific to .NET Framework thread sampling.
Enable `debug` level logging in the native profiler to see detailed stack
capture diagnostics. You can do this by setting the environment variable
`OTEL_LOG_LEVEL=debug` when the instrumented application starts.

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
[debug] [StackCapture] [debug] [StackCapture] Unseeded capture failed (0x80004005). ThreadID=...
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

1. Ensure thread sampling is enabled in the plugin configuration
2. Check that the profiler is successfully attached (look for
   `ContinuousProfiler sampling thread started.` in the logs)

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
and exports in the way defined by the plugin.

### Requirements

* .NET 8.0 or higher

> [!NOTE]
> Allocation sampling is not supported on .NET Framework. The runtime does not
> include the low-level profiling APIs needed to observe individual memory
> allocations. The settings related to allocation sampling in the plugin configuration
> will be ignored when running on .NET Framework, but the same plugin can be
> used for both .NET and .NET Framework without modification.

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

## Feature support matrix

| Feature             | .NET 8.0+ | .NET Framework 4.6.2+ |
|---------------------|-----------|-----------------------|
| Thread sampling     | Supported | Supported             |
| Allocation sampling | Supported | Not supported         |

## Plugin

For now, the plugin is responsible for

* defining configuration for continuous profiling
* providing exporter for the allocation and profiling data
* *parsing data* prepared by the native code.

### Plugin contract

> [!IMPORTANT]  
> It will be subject to change, when <https://github.com/open-telemetry/oteps/pull/239>
> or <https://github.com/open-telemetry/oteps/pull/237> will be ready and merged.

Implement
`IContinuousProfilerPlugin.GetFirstContinuousProfilerConfiguration()` and
return a `ContinuousProfilerConfiguration`. Only the first plugin that implements
this interface is used. See the [continuous profiling plugin
example](../plugins.md#continuous-profiling).

> [!NOTE]  
> On .NET Framework, the `allocationSamplingEnabled` setting is ignored since
> allocation sampling is not supported. The same plugin configuration works
> for both .NET and .NET Framework - thread sampling will be enabled on both
> platforms when configured.

### Exporter contract

The exporter must implement two methods:

```csharp
public void ExportThreadSamples(byte[] buffer, int read, uint samplingInterval, CancellationToken cancellationToken);
public void ExportAllocationSamples(byte[] buffer, int read, CancellationToken cancellationToken);
```

Both accept buffer produced by the native code, the length of filled
data, and cancellation token. `ExportThreadSamples` also receives the sampling
interval in milliseconds that was used to produce that specific batch.
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
