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

* .NET 6.0 or higher (`ICorProfilerInfo12` available in runtime).
* .NET Framework is not supported, as `ICorProfiler10` and `ICorProfiler12`
  are not available in .NET Fx.

Note that `ICorProfiler10` can be used, but .NET Core 3.1 or .NET 5.0 aren't
supported by the OpenTelemetry .NET Automatic Instrumentation.

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

None of the .NET Framework versions is supported. You have to switch
to a supported .NET version.

#### Can I tell the sampler to ignore some threads?

There is no such functionality. All managed threads are captured by the profiler.

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

* .NET 6.0 or higher (`ICorProfilerInfo12` available in runtime) - technically
  it could be .NET5 which is not supported by OTel/MS.

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

None of the .NET Framework versions is supported. You have to switch to
supported .NET version.

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
    var allocationSamplingEnabled = true; // enables allocation sampling
    var maxMemorySamplesPerMinute = 200u; // max number of samples in minutes. 200 is tested default value by Splunk.
    var exportInterval = TimeSpan.FromMilliseconds(500); // Pause time before next execution of exporting/reading buffer  process
    var exportTimeout = TimeSpan.FromMilliseconds(500); // Export timeout
    object continuousProfilerExporter = new ConsoleExporter();
    return Tuple.Create(threadSamplingEnabled, threadSamplingInterval, allocationSamplingEnabled, maxMemorySamplesPerMinute, exportInterval, exportTimeout, continuousProfilerExporter);
}
```

if more than one plugin implement `GetContinuousProfilerConfiguration` only
the first one will be used. Other will be ignored.

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

### Native parser

As there is no default OpenTelemetry Protocol format there is not easy way to
create good contract between OpenTelemetry Automatic Instrumentation and
the plugin. The plugin has to implement (copy) our version of the parser.
It should be changed when the OTel Proposal will be merged, and we can start implementing
real OTLP exporter.

Implementation can be found in [`SampleNativeFormatParser`](../../test/test-applications/integrations/TestApplication.ContinuousProfiler/Exporter/SampleNativeFormatParser.cs).
