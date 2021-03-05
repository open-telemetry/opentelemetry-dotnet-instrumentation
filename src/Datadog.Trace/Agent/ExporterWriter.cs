using System;
using System.Linq;
using System.Threading.Tasks;
using Datadog.Trace.DogStatsd;
using Datadog.Trace.Logging;
using Datadog.Trace.Util;
using Datadog.Trace.Vendors.StatsdClient;

namespace Datadog.Trace.Agent
{
    internal class ExporterWriter : ITraceWriter
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<ExporterWriter>();

        private readonly ExporterWriterBuffer<Span[]> _tracesBuffer;
        private readonly IDogStatsd _statsd;
        private readonly Task _flushTask;
        private readonly TaskCompletionSource<bool> _processExit = new TaskCompletionSource<bool>();

        private readonly IExporter _exporter;

        public ExporterWriter(IExporter exporter, IDogStatsd statsd, bool automaticFlush = true, int queueSize = 1000)
        {
            _tracesBuffer = new ExporterWriterBuffer<Span[]>(queueSize);
            _exporter = exporter;
            _statsd = statsd;

            _flushTask = automaticFlush ? Task.Run(FlushTracesTaskLoopAsync) : Task.FromResult(true);
        }

        public Task<bool> Ping()
        {
            return _exporter.SendTracesAsync(ArrayHelper.Empty<Span[]>());
        }

        public void WriteTrace(Span[] trace)
        {
            var success = _tracesBuffer.Push(trace);

            if (!success)
            {
                Log.Warning("Trace buffer is full. Dropping a trace from the buffer.");
            }

            if (_statsd != null)
            {
                _statsd.Increment(TracerMetricNames.Queue.EnqueuedTraces);
                _statsd.Increment(TracerMetricNames.Queue.EnqueuedSpans, trace.Length);

                if (!success)
                {
                    _statsd.Increment(TracerMetricNames.Queue.DroppedTraces);
                    _statsd.Increment(TracerMetricNames.Queue.DroppedSpans, trace.Length);
                }
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

            if (_statsd != null)
            {
                var spanCount = traces.Sum(t => t.Length);

                _statsd.Increment(TracerMetricNames.Queue.DequeuedTraces, traces.Length);
                _statsd.Increment(TracerMetricNames.Queue.DequeuedSpans, spanCount);
            }

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
