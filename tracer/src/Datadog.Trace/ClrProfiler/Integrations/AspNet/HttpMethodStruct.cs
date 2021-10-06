#if NETFRAMEWORK
using System.ComponentModel;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.Integrations.AspNet
{
    /// <summary>
    /// Http method struct copy target for ducktyping
    /// </summary>
    [DuckCopy]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct HttpMethodStruct
    {
        /// <summary>
        /// Gets the http method in string
        /// </summary>
        public string Method;
    }
}
#endif
