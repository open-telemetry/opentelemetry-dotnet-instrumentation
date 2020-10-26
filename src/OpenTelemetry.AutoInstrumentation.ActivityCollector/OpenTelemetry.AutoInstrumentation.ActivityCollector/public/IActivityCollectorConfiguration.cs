using System;
using OpenTelemetry.AutoInstrumentation.ActivityExporter;
using OpenTelemetry.DynamicActivityBinding;

namespace OpenTelemetry.AutoInstrumentation.ActivityCollector
{
    public interface IActivityCollectorConfiguration
    {
        TimeSpan ExportInterval { get; }
        int ExportBatchSizeCap { get; }
        bool AggregateActivitiesIntoTraces { get; }
        IActivityExporter ActivityExporter { get; }

        Action<ActivityStub> OnActivityStartedProcessor { get; }
        Action<ActivityStub> OnActivityStoppedProcessor { get; }
    }
}
