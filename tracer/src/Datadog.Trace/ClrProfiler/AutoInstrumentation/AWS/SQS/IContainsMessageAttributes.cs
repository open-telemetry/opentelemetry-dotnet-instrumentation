using System.Collections;
using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AWS.SQS
{
    /// <summary>
    /// MessageAttributes interface for ducktyping
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IContainsMessageAttributes
    {
        /// <summary>
        /// Gets or sets the message attributes
        /// </summary>
        IDictionary MessageAttributes { get; set;  }
    }
}
