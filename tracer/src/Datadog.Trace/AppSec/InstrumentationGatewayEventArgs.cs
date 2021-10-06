using System;
using System.Collections.Generic;
using Datadog.Trace.AppSec.Transport;

namespace Datadog.Trace.AppSec
{
    internal class InstrumentationGatewayEventArgs : EventArgs
    {
        public InstrumentationGatewayEventArgs(IDictionary<string, object> eventData, ITransport transport, Span relatedSpan)
        {
            EventData = eventData;
            Transport = transport;
            RelatedSpan = relatedSpan;
        }

        public IDictionary<string, object> EventData { get; }

        public ITransport Transport { get; }

        public Span RelatedSpan { get; }
    }
}
