using System;
using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing.XUnit
{
    /// <summary>
    /// Exception aggregator interface
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IExceptionAggregator
    {
        /// <summary>
        /// Extract exception
        /// </summary>
        /// <returns>Exception instance</returns>
        Exception ToException();
    }
}
