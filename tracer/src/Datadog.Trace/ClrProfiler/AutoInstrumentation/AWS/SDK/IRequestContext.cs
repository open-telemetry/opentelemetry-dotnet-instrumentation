using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AWS.SDK
{
    /// <summary>
    /// IRequestContext interface for ducktyping
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRequestContext
    {
        /// <summary>
        /// Gets the client config
        /// </summary>
        IClientConfig ClientConfig { get; }

        /// <summary>
        /// Gets the Request
        /// </summary>
        IRequest Request { get; }
    }
}
