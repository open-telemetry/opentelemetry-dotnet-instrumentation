using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Kafka
{
    /// <summary>
    /// ProduceException interface for duck-typing
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IProduceException
    {
        /// <summary>
        /// Gets the delivery result associated with the produce request
        /// </summary>
        public IDeliveryResult DeliveryResult { get; }
    }
}
