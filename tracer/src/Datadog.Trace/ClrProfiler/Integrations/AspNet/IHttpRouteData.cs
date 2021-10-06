#if NETFRAMEWORK
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System.Collections.Generic;
using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.Integrations.AspNet
{
    /// <summary>
    /// IHttpRouteData interface for ducktyping
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IHttpRouteData
    {
        IHttpRoute Route { get; }

        IDictionary<string, object> Values { get; }
    }
}
#endif
