using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.RabbitMQ
{
    /// <summary>
    /// Body interface for ducktyping
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IBody
    {
        /// <summary>
        /// Gets the length of the message body
        /// </summary>
        int Length { get; }
    }
}
