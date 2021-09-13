using System;
using System.Linq;
using System.Threading.Tasks;
using Datadog.Trace.Abstractions;
using Datadog.Trace.DogStatsd;
using Datadog.Trace.Logging;
using Datadog.Trace.Util;

namespace Datadog.Trace.Agent
{
    internal class ExporterWriter : ITraceWriter
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<ExporterWriter>();

        private readonly ExporterWriterBuffer<Span[]> _tracesBuffer;
        private readonly IMetrics _metrics;
        private readonly Task _flushTask;
        private readonly TaskCompletionSource<bool> _processExit = new TaskCompletionSource<bool>();

        private readonly IExporter _exporter;

        public ExporterWriter(IExporter exporter, IMetrics metrics, bool automaticFlush = true, int queueSize = 1000)
        {
            _tracesBuffer = new ExporterWriterBuffer<Span[]>(queueSize);
            _exporter = exporter;
            _metrics = metrics;

            _flushTask = automaticFlush ? Task.Run(FlushTracesTaskLoopAsync) : Task.FromResult(true);
        }

        public Task<bool> Ping()
        {
            return _exporter.SendTracesAsync(ArrayHelper.Empty<Span[]>());
        }

        public void WriteTrace(ArraySegment<Span> trace)
        {
            // TODO: Simple solution when pulling from upstream: copy the segment contents
            // to an array. Review this code and optimize as appropriate.
            var success = _tracesBuffer.Push(trace.ToArray());

            if (!success)
            {
                Log.Warning("Trace buffer is full. Dropping a trace from the buffer.");
            }

            _metrics.Increment(TracerMetricNames.Queue.EnqueuedTraces);
            _metrics.Increment(TracerMetricNames.Queue.EnqueuedSpans, trace.Count);

            if (!success)
            {
                _metrics.Increment(TracerMetricNames.Queue.DroppedTraces);
                _metrics.Increment(TracerMetricNames.Queue.DroppedSpans, trace.Count);
            }
        }

        public async Task FlushAndCloseAsync()
        {
            if (!_processExit.TrySetResult(true))
            {
                return;
            }

            await Task.WhenAny(_flushTask, Task.Delay(TimeSpan.FromSeconds(20)))
                      .ConfigureAwait(false);

            if (!_flushTask.IsCompleted)
            {
                Log.Warning("Could not flush all traces before process exit");
            }
        }

        public async Task FlushTracesAsync()
        {
            var traces = _tracesBuffer.Pop();

            var spanCount = traces.Sum(t => t.Length);
            _metrics.Increment(TracerMetricNames.Queue.DequeuedTraces, traces.Length);
            _metrics.Increment(TracerMetricNames.Queue.DequeuedSpans, spanCount);

            if (traces.Length > 0)
            {
                await _exporter.SendTracesAsync(traces).ConfigureAwait(false);
            }
        }

        private async Task FlushTracesTaskLoopAsync()
        {
            while (true)
            {
                try
                {
                    await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(1)), _processExit.Task)
                              .ConfigureAwait(false);

                    if (_processExit.Task.IsCompleted)
                    {
                        await FlushTracesAsync().ConfigureAwait(false);
                        return;
                    }
                    else
                    {
                        await FlushTracesAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An unhandled error occurred during the flushing task");
                }
            }
        }
    }
}
