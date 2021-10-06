using System.ComponentModel;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Http.HttpClient
{
    /// <summary>
    /// HttpResponseMessage interface for ducktyping
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IHttpResponseMessage : IDuckType
    {
        /// <summary>
        /// Gets the status code of the http response
        /// </summary>
        int StatusCode { get; }
    }
}
