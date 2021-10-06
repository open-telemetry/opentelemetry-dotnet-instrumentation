using System;
using System.ComponentModel;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Kafka
{
    /// <summary>
    /// TypedDeliveryHandlerShim_Action for duck-typing
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITypedDeliveryHandlerShimAction
    {
        /// <summary>
        /// Sets the delivery report handler
        /// </summary>
        [DuckField]
        public object Handler { set; }
    }
}
