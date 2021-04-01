using System;
using Datadog.Trace.Headers;
using Datadog.Trace.Propagation;
using OpenTracing.Propagation;

namespace Datadog.Trace.OpenTracing
{
    internal class HttpHeadersCodec : ICodec
    {
        private readonly IPropagator _propagator;

        public HttpHeadersCodec(IPropagator propagator)
        {
            _propagator = propagator;
        }

        public global::OpenTracing.ISpanContext Extract(object carrier)
        {
            var map = carrier as ITextMap;

            if (map == null)
            {
                throw new ArgumentException("Carrier should have type ITextMap", nameof(carrier));
            }

            IHeadersCollection headers = new TextMapHeadersCollection(map);
            var propagationContext = _propagator.Extract(headers);

            return new OpenTracingSpanContext(propagationContext);
        }

        public void Inject(global::OpenTracing.ISpanContext context, object carrier)
        {
            var map = carrier as ITextMap;

            if (map == null)
            {
                throw new ArgumentException("Carrier should have type ITextMap", nameof(carrier));
            }

            IHeadersCollection headers = new TextMapHeadersCollection(map);

            if (context is OpenTracingSpanContext otSpanContext && otSpanContext.Context is SpanContext spanContext)
            {
                _propagator.Inject(spanContext, headers);
            }
            else
            {
                // TODO: Consider using OpenTracing headers
                headers.Set(DDHttpHeaderNames.TraceId, context.TraceId);
                headers.Set(DDHttpHeaderNames.ParentId, context.SpanId);
            }
        }
    }
}
