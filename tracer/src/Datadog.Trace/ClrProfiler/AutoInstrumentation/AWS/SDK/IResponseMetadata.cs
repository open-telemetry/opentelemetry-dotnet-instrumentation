using System.Collections.Generic;
using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AWS.SDK
{
    /// <summary>
    /// ResponseMetadata interface for ducktyping
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IResponseMetadata
    {
        /// <summary>
        /// Gets the ID of the request
        /// </summary>
        string RequestId { get; }

        /// <summary>
        /// Gets the metadata associated with the request
        /// </summary>
        IDictionary<string, string> Metadata { get; }
    }
}
