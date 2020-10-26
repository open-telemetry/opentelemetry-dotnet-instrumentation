using System;
using System.Collections.Generic;
using OpenTelemetry.AutoInstrumentation.ActivityCollector;
using OpenTelemetry.DynamicActivityBinding;

namespace OpenTelemetry.AutoInstrumentation.ActivityExporter
{
    /// <summary>
    /// An activity exporter receives a batch of spans OR local traces.
    /// (A span is an are Activity (aka sub-operation);
    /// a local trace is a subset of a distributed trace that contains all its sub-operations non-reentrantly
    /// executed within the current process.)
    /// 
    /// An activity exporter performs all tasks that need to happen to spans after they were collected.
    /// Examples for such tasks are:
    ///  - Sample spans (we may additionally support “early” sampling via using the ActivityListener SampleXxx methods in the future).
    ///  - Enrich spans with additional / vendor-specific standard tags to describe the environment, service or machine information.
    ///  - Serialize spans.
    ///  - Send spans to the remote ingestion endpoint, including any associated buffering or re-try logic.
    ///  
    /// The ExportXxx methods should synchronously complete within a time significantly less than the export interval.
    /// See: <see cref="CollectAndExportBackgroundLoop.CollectAndExportBackgroundLoop(TimeSpan, int, bool, IActivityExporter)"/>,
    /// <see cref="IActivityCollectorConfiguration.ExportInterval"/>)
    /// 
    /// The ExportXxx methods may or may not use async IO methods if they do NOT use the managed thread pool.
    /// Avoid using the managed thread pool in the exporter for reasons described in code comments
    /// for <see cref="CollectAndExportBackgroundLoop.Start"/>!
    /// 
    /// If the ExportXxx methods are not certain to complete significantly faster than the export interval, use a dedicated,
    /// explicitly created thread(see code and comments for <see cref="CollectAndExportBackgroundLoop.Start"/> for details).
    /// 
    /// Wherever possible, activity enrichment (e.g.adding information to describe the environment, service or machine information)
    /// should happen within the scope of the ExportXxx methods and after the sampling has been done.
    /// In very specific circumstances where such enrichment must happen at the beginning of the activity life time,
    /// or synchronously to starting or stopping the activity,
    /// use <see cref="IActivityCollectorConfiguration.OnActivityStartedProcessor"/> '
    /// and <see cref="IActivityCollectorConfiguration.OnActivityStoppedProcessor"/> 
    /// on <see cref="IActivityCollectorConfiguration"/>.
    /// 
    /// Implementations of IActivityExporter should subclass from ActivityExporterBase when possible,
    /// but direct implementations are also acceptable.

    /// </summary>
    public interface IActivityExporter : IDisposable
    {
        bool IsExportTracesSupported { get; }

        bool IsExportActivitiesSupported { get; }

        ExportResult ExportTraces(IReadOnlyCollection<LocalTrace> traces);

        ExportResult ExportActivities(IReadOnlyCollection<ActivityStub> traces);

        void Shutdown();
    }
}
