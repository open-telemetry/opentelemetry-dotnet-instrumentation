using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Kafka
{
    /// <summary>
    /// Message interface for duck-typing
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IMessage
    {
        /// <summary>
        /// Gets the value of the message
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Gets the timestamp that the message was produced
        /// </summary>
        public ITimestamp Timestamp { get; }

        /// <summary>
        /// Gets or sets the headers for the record
        /// </summary>
        public IHeaders Headers { get; set; }
    }
}
