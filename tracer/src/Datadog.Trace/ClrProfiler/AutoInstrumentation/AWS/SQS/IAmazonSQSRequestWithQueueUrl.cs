using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AWS.SQS
{
    /// <summary>
    /// Interface for ducktyping AmazonSQSRequest implementations with the QueueUrl property
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IAmazonSQSRequestWithQueueUrl
    {
        /// <summary>
        /// Gets the URL of the queue
        /// </summary>
        string QueueUrl { get; }
    }
}
