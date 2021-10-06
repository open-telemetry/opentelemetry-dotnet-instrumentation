#if NETFRAMEWORK
using System.ComponentModel;
using System.Web;
using System.Web.Routing;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.Integrations.AspNet
{
    /// <summary>
    /// ControllerContext struct copy target for ducktyping
    /// </summary>
    [DuckCopy]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct ControllerContextStruct
    {
        /// <summary>
        /// Gets the HttpContext
        /// </summary>
        public HttpContextBase HttpContext;

        /// <summary>
        /// Gets the RouteData
        /// </summary>
        public RouteData RouteData;
    }
}
#endif
