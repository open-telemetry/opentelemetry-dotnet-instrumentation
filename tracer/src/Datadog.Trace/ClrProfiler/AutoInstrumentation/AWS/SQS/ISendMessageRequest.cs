using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AWS.SQS
{
    /// <summary>
    /// SendMessageRequest interface for ducktyping
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ISendMessageRequest : IAmazonSQSRequestWithQueueUrl, IContainsMessageAttributes
    {
    }
}
