using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Trace.Abstractions;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Trace
{
    /// <summary>
    /// A scope is a handle used to manage the concept of an active span.
    /// Meaning that at a given time at most one span is considered active and
    /// all newly created spans that are not created with the ignoreActiveSpan
    /// parameter will be automatically children of the active span.
    /// </summary>
    public class Scope : IScope
    {
        private readonly IScopeManager _scopeManager;
        private readonly bool _finishOnClose;

        internal Scope(Scope parent, Activity activity, IScopeManager scopeManager, bool finishOnClose)
        {
            Parent = parent;
            Activity = activity;
            _scopeManager = scopeManager;
            _finishOnClose = finishOnClose;
        }

        /// <summary>
        /// Gets the active span wrapped in this scope
        /// </summary>
        public Activity Activity { get; }

        internal Scope Parent { get; }

        /// <summary>
        /// Closes the current scope and makes its parent scope active
        /// </summary>
        public void Close()
        {
            _scopeManager.Close(this);

            if (_finishOnClose)
            {
                Activity.Dispose();
            }
        }

        /// <summary>
        /// Closes the current scope and makes its parent scope active
        /// </summary>
        public void Dispose()
        {
            try
            {
                Close();
            }
            catch
            {
                // Ignore disposal exceptions here...
                // TODO: Log? only in test/debug? How should Close() concerns be handled (i.e. independent?)
            }
        }
    }
}
