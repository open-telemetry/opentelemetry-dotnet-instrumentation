using System.Collections;
using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AWS.SQS
{
    /// <summary>
    /// SendMessageBatchRequest interface for ducktyping
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ISendMessageBatchRequest : IAmazonSQSRequestWithQueueUrl
    {
        /// <summary>
        /// Gets the message entries
        /// </summary>
        IList Entries { get; }
    }
}
