using System;
using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Trace.Abstractions;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Trace
{
    internal abstract class ScopeManagerBase : IScopeManager, IScopeRawAccess
    {
        public event EventHandler<ActivityEventArgs> TraceStarted;

        public event EventHandler<ActivityEventArgs> SpanOpened;

        public event EventHandler<ActivityEventArgs> SpanActivated;

        public event EventHandler<ActivityEventArgs> SpanDeactivated;

        public event EventHandler<ActivityEventArgs> SpanClosed;

        public event EventHandler<ActivityEventArgs> TraceEnded;

        public abstract Scope Active { get; protected set; }

        Scope IScopeRawAccess.Active
        {
            get => Active;
            set => Active = value;
        }

        public Scope Activate(Activity activity, bool finishOnClose)
        {
            var newParent = Active;

            var scope = new Scope(newParent, activity, this, finishOnClose);
            var scopeOpenedArgs = new ActivityEventArgs(activity);

            return Activate(scope, newParent, scopeOpenedArgs);
        }

        public void Close(Scope scope)
        {
            var current = Active;
            var isRootSpan = scope.Parent == null;

            if (current == null || current != scope)
            {
                // This is not the current scope for this context, bail out
                SpanClosed?.Invoke(this, new ActivityEventArgs(scope.Activity));
                return;
            }

            // if the scope that was just closed was the active scope,
            // set its parent as the new active scope
            Active = scope.Parent;
            SpanDeactivated?.Invoke(this, new ActivityEventArgs(scope.Activity));

            if (!isRootSpan)
            {
                SpanActivated?.Invoke(this, new ActivityEventArgs(scope.Parent.Activity));
            }

            SpanClosed?.Invoke(this, new ActivityEventArgs(scope.Activity));

            if (isRootSpan)
            {
                TraceEnded?.Invoke(this, new ActivityEventArgs(scope.Activity));
            }
        }

        private Scope Activate(Scope scope, Scope newParent, ActivityEventArgs scopeOpenedArgs)
        {
            if (newParent == null)
            {
                TraceStarted?.Invoke(this, scopeOpenedArgs);
            }

            SpanOpened?.Invoke(this, scopeOpenedArgs);

            Active = scope;

            if (newParent != null)
            {
                SpanDeactivated?.Invoke(this, new ActivityEventArgs(newParent.Activity));
            }

            SpanActivated?.Invoke(this, scopeOpenedArgs);

            return scope;
        }
    }
}
