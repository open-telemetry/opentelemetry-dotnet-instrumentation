using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Kafka
{
    /// <summary>
    /// ConsumeException interface for duck-typing
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IConsumeException
    {
        /// <summary>
        /// Gets the consume result associated with the consume request
        /// </summary>
        public IConsumeResult ConsumerRecord { get; }
    }
}
