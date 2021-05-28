using System;
using System.Diagnostics;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Trace.Abstractions
{
    /// <summary>
    /// Interface for managing a scope.
    /// </summary>
    internal interface IScopeManager
    {
        event EventHandler<ActivityEventArgs> TraceStarted;

        event EventHandler<ActivityEventArgs> SpanOpened;

        event EventHandler<ActivityEventArgs> SpanActivated;

        event EventHandler<ActivityEventArgs> SpanDeactivated;

        event EventHandler<ActivityEventArgs> SpanClosed;

        event EventHandler<ActivityEventArgs> TraceEnded;

        Scope Active { get; }

        Scope Activate(Activity activity, bool finishOnClose);

        void Close(Scope scope);
    }
}
