using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AWS.SDK
{
    /// <summary>
    /// IExecutionContext interface for ducktyping
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IExecutionContext
    {
        /// <summary>
        /// Gets the RequestContext
        /// </summary>
        IRequestContext RequestContext { get; }

        /// <summary>
        /// Gets the ResponseContext
        /// </summary>
        IResponseContext ResponseContext { get; }
    }
}
