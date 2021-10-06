using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AWS.SDK
{
    /// <summary>
    /// IRequest interface for ducktyping
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRequest
    {
        /// <summary>
        /// Gets the HTTP method
        /// </summary>
        string HttpMethod { get; }
    }
}
