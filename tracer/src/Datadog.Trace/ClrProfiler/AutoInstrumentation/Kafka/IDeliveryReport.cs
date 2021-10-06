using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Kafka
{
    /// <summary>
    /// DeliveryReport interface for duck-typing
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IDeliveryReport : IDeliveryResult
    {
        /// <summary>
        /// Gets the Error associated with the delivery report
        /// </summary>
        public IError Error { get; }
    }
}
