using System;
using System.Collections.Generic;
using System.Threading;
using Datadog.Util;
using OpenTelemetry.AutoInstrumentation.ActivityCollector;
using OpenTelemetry.DynamicActivityBinding;

using Log = OpenTelemetry.AutoInstrumentation.ActivityCollector.Log;

namespace OpenTelemetry.AutoInstrumentation.ActivityExporter
{
    public abstract class ActivityExporterBase : IActivityExporter
    {
        private bool _hasShutdown;
        private ManualResetEventSlim _shutdownMonitor;

        protected ActivityExporterBase(bool isExportTracesSupported, bool isExportActivitiesSupported)
        {
            this.IsExportTracesSupported = isExportTracesSupported;
            this.IsExportActivitiesSupported = isExportActivitiesSupported;
            _hasShutdown = false;
            _shutdownMonitor = null;
        }

        public bool IsExportTracesSupported { get; }

        public bool IsExportActivitiesSupported { get; }

        public ExportResult ExportTraces(IReadOnlyCollection<LocalTrace> traces)
        {
            if (!IsExportTracesSupported)
            {
                throw new NotSupportedException($"This {this.GetType().Name} does not support {nameof(ExportTraces)}.");
            }

            Log.Debug(nameof(ActivityExporterBase),
                      $"Starting {nameof(ExportTraces)}",
                      "ExporterType", this.GetType().FullName,
                      $"{nameof(traces)}?.Count", traces?.Count);

            if (traces == null || traces.Count == 0)
            {
                return ExportResult.CreateSuccess(isTraceExport: true, 0, 0);
            }

            return ExportTracesImpl(traces);
        }

        public ExportResult ExportActivities(IReadOnlyCollection<ActivityStub> spans)
        {
            if (!IsExportActivitiesSupported)
            {
                throw new NotSupportedException($"This {this.GetType().Name} does not support {nameof(ExportActivities)}.");
            }

            Log.Debug(nameof(ActivityExporterBase),
                      $"Starting {nameof(ExportActivities)}",
                      "ExporterType", this.GetType().FullName,
                      $"{nameof(spans)}?.Count", spans?.Count);

            if (spans == null || spans.Count == 0)
            {
                return ExportResult.CreateSuccess(isTraceExport: false, 0, 0);
            }

            return ExportActivitiesImpl(spans);
        }

        public void Shutdown()
        {
            if (_hasShutdown)
            {
                return;
            }

            using (var newMonitor = new ManualResetEventSlim())
            {
                ManualResetEventSlim chosenMonitor = Concurrent.TrySetOrGetValue(ref _shutdownMonitor, newMonitor);

                if (chosenMonitor == newMonitor)
                {
                    Log.Debug(nameof(ActivityExporterBase),
                              $"Starting {nameof(Shutdown)}",
                              "ExporterType", this.GetType().FullName);

                    ShutdownImpl();
                    _hasShutdown = true;
                    chosenMonitor.Set();

                    Interlocked.Exchange(ref _shutdownMonitor, null);
                }
                else
                {
                    try
                    {
                        chosenMonitor.Wait();
                    }
                    catch (ObjectDisposedException)
                    { }
                }
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Shutdown();
            }
        }

        protected abstract ExportResult ExportActivitiesImpl(IReadOnlyCollection<ActivityStub> spans);

        protected abstract ExportResult ExportTracesImpl(IReadOnlyCollection<LocalTrace> traces);

        protected abstract void ShutdownImpl();

    }
}
