# Selective sampling

> [!IMPORTANT]
> Selective sampling is an experimental feature.

This feature builds on top of the continuous profiling capabilities.
See [Continuous profiling](./continuous-profiler.md) for more details.

Selective sampling API allows you to enable span-based sampling.
After span is selected for frequent sampling, any thread associated with
the span context will be sampled, until span is deselected.

Span can be selected for frequent sampling by calling:
```csharp
SelectiveSamplingStart(ActivityTraceId traceId)
  ```

Span is removed from selection by calling:
```csharp
SelectiveSamplingStop(ActivityTraceId traceId)
  ```

You can enable frequent sampling using the custom plugin, which
can parse thread sampling data and export it.

## How does the thread sampler work?

The profiler uses the
[.NET profiler](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/)
to perform periodic call stack sampling.

The native-side sampling thread, and managed-side exporter thread
are shared with continuous profiler, if it is enabled.

Continuous profiling and selective sampling can be enabled independently.

## Requirements

* .NET 8.0 or higher (`ICorProfilerInfo12` available in runtime).
* .NET Framework is not supported, as `ICorProfiler10` and `ICorProfiler12`
  are not available in .NET Fx.

## Enable the frequent sampling

In order to enable the frequent sampling, implement following method in your custom plugin:

```csharp
    public Tuple<uint, TimeSpan, TimeSpan, object> GetSelectiveSamplingConfiguration()
```

Fields in the returned tuple are expected to be:
- `uint` - sampling period in milliseconds (values lower than 50ms are not recommended due to sampling overhead).
- `TimeSpan` - export interval.
- `TimeSpan` - export timeout.
- `object` - exporter instance, expected to contain `ExportSelectedThreadSamples` method.

```csharp
public void ExportSelectedThreadSamples(byte[] buffer, int read, CancellationToken cancellationToken)
```
The `buffer` contains thread samples encoded by the profiler, `read` is the number of bytes read into the buffer.

See [`TestApplication.SelectiveSampler`](../../test/test-applications/integrations/TestApplication.SelectiveSampler)
for sample implementation of plugin.

## Limits

- Instrumentation can't guarantee that configured sampling period will always be respected
(timestamps encoded in samples buffer mark points in time when samples were collected).
- Spans selected for frequent sampling more than 15 minutes in the past will be considered stale
  and will be automatically deselected.
- Number of spans selected for frequent sampling is limited to 50 at a time.

Thread sampling supported by the continuous profiler, and selective sampling share native-side
sampling thread, and managed-side exporter thread.

Considering both types of profiling can configure export interval and export timeout, the following rules apply:
- If different export intervals are configured, the higher export interval will be used.
- If different export timeouts are configured, the higher export timeout will be used.

Additional restrictions apply to sampling intervals:
- Continuous profiling interval is required to be higher than selective sampling interval.
- Continuous profiling interval is required to be a multiple of selective sampling interval.

In order to avoid unnecessary overhead, if both continuous profiling and selective sampling
are enabled, during collection of the call stacks for all the threads, if thread is associated
with a span selected for frequent sampling, collected sample will have additional flag set.
Exporter can use this flag to identify samples associated with spans selected for frequent sampling.

See [`SampleNativeFormatParser`](../../test/test-applications/integrations/TestApplication.ContinuousProfiler/Exporter/SampleNativeFormatParser.cs) for sample implementation.