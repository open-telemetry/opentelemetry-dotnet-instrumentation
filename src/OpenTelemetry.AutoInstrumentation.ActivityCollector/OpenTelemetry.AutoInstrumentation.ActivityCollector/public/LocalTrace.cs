using System;
using System.Collections.Generic;
using Datadog.Collections;
using OpenTelemetry.DynamicActivityBinding;

namespace OpenTelemetry.AutoInstrumentation.ActivityCollector
{
    /// <summary>
    /// Represents a "local trace".
    /// This is a subset of the spans belonging to a distributed tras that represent a single non-reentrant invocation of the local application component.
    /// The Id of the overall global trace is the trace Id.
    /// The Id of the local trace is the span id of the local root span. This is because a global trace may be re-entrant and we need to uniquely identify
    /// the non-reentrant subset.
    /// </summary>
    public struct LocalTrace
    {
        private static int ActivitiesCollectionSegmentSize = 6;

        private readonly GrowingCollection<ActivityStub> _activities;
        private readonly ActivityStub _root;
        private readonly ulong _localTraceId;

        public LocalTrace(ActivityStub rootActivity, ulong rootActivitySpanIdHash)
        {
            _root = rootActivity;
            _localTraceId = rootActivitySpanIdHash;

            _activities = new GrowingCollection<ActivityStub>(ActivitiesCollectionSegmentSize);
            _activities.Add(rootActivity);
        }

        public IReadOnlyCollection<ActivityStub> Activities
        {
            get { return _activities; }
        }

        public ActivityStub LocalRoot
        {
            get { return _root; }
        }

        public ulong LocalTraceId
        {
            get { return _localTraceId; }
        }

        public void Add(ActivityStub activity)
        {
            _activities.Add(activity);
        }
    }
}
