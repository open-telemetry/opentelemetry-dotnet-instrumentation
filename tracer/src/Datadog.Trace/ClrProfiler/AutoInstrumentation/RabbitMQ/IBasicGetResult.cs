using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.RabbitMQ
{
    /// <summary>
    /// BasicGetResult interface for ducktyping
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IBasicGetResult
    {
        /// <summary>
        /// Gets the message body of the result
        /// </summary>
        IBody Body { get; }

        /// <summary>
        /// Gets the message properties
        /// </summary>
        IBasicProperties BasicProperties { get; }
    }
}
