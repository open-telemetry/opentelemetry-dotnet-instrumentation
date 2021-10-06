using System;
using System.ComponentModel;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Http.HttpClient
{
    /// <summary>
    /// HttpRequestMessage interface for ducktyping
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IHttpRequestMessage : IDuckType
    {
        /// <summary>
        /// Gets the Http Method
        /// </summary>
        HttpMethodStruct Method { get; }

        /// <summary>
        /// Gets the request uri
        /// </summary>
        Uri RequestUri { get; }

        /// <summary>
        /// Gets the request headers
        /// </summary>
        IRequestHeaders Headers { get; }
    }
}
