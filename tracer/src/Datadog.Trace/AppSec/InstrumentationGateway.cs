using System;
using System.Collections.Generic;
using Datadog.Trace.AppSec.Transport;

namespace Datadog.Trace.AppSec
{
    internal class InstrumentationGateway
    {
        public event EventHandler<InstrumentationGatewayEventArgs> InstrumentationGatewayEvent;

        public void RaiseEvent(IDictionary<string, object> eventData, ITransport transport, Span relatedSpan)
        {
            InstrumentationGatewayEvent?.Invoke(this, new InstrumentationGatewayEventArgs(eventData, transport, relatedSpan));
        }
    }
}
