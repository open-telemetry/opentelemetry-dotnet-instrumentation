using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AWS.SDK
{
    /// <summary>
    /// IResponseContext interface for ducktyping
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IResponseContext
    {
        /// <summary>
        /// Gets the SDK response
        /// </summary>
        IAmazonWebServiceResponse Response { get; }
    }
}
