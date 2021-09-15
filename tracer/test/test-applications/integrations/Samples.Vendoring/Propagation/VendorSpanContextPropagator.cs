using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace;
using Datadog.Trace.Conventions;
using Datadog.Trace.Propagation;

namespace Samples.Vendoring.Propagation
{
    internal class VendorSpanContextPropagator : IPropagator
    {
        private const string TraceIdHeader = "vendor-trace-id";
        private const string SpanIdHeader = "vendor-span-id";

        private readonly ITraceIdConvention _traceIdConvention;

        public VendorSpanContextPropagator(ITraceIdConvention traceIdConvention)
        {
            _traceIdConvention = traceIdConvention;
        }

        public SpanContext Extract<T>(T carrier, Func<T, string, IEnumerable<string>> getter)
        {
            var traceId = ParseTraceId(carrier, getter);

            if (traceId == TraceId.Zero)
            {
                // a valid traceId is required to use distributed tracing
                return null;
            }

            var spanId = ParseSpanId(carrier, getter);

            return new SpanContext(traceId, spanId);
        }

        public void Inject<T>(SpanContext context, T carrier, Action<T, string, string> setter)
        {
            setter(carrier, TraceIdHeader, context.TraceId.ToString());
            setter(carrier, SpanIdHeader, context.SpanId.ToString());
        }

        private TraceId ParseTraceId<T>(T carrier, Func<T, string, IEnumerable<string>> getter)
        {
            var headerValue = getter(carrier, TraceIdHeader).FirstOrDefault();
            if (headerValue == null)
            {
                return TraceId.Zero;
            }

            return _traceIdConvention.CreateFromString(headerValue);
        }

        private ulong ParseSpanId<T>(T carrier, Func<T, string, IEnumerable<string>> getter)
        {
            var headerValue = getter(carrier, SpanIdHeader).FirstOrDefault();
            if (headerValue == null)
            {
                return default;
            }

            ulong.TryParse(headerValue, out ulong spanId);
            return spanId;
        }
    }
}
