using System;
using System.Diagnostics;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Trace
{
    /// <summary>
    /// EventArgs for a Span
    /// </summary>
    internal class ActivityEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityEventArgs"/> class.
        /// Creates a new <see cref="ActivityEventArgs"/> using <paramref name="activity"/>
        /// </summary>
        /// <param name="activity">The <see cref="Activity"/> used to initialize the <see cref="ActivityEventArgs"/> object.</param>
        public ActivityEventArgs(Activity activity) => Activity = activity;

        internal Activity Activity { get; }
    }
}
