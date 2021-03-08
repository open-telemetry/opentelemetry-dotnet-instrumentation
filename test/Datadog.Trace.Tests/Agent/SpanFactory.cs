using Datadog.Trace.Tagging;
using Moq;

namespace Datadog.Trace.Tests.Agent
{
    public static class SpanFactory
    {
        public static Span CreateSpan()
        {
            var parentSpanContext = new Mock<ISpanContext>();
            var traceContext = new Mock<ITraceContext>();
            var spanContext = new SpanContext(parentSpanContext.Object, traceContext.Object, serviceName: null);

            var additionalTags = new CommonTags();

            additionalTags.Version = "v1.0";
            additionalTags.Environment = "Test";

            var span = new Span(spanContext, start: null, tags: additionalTags);
            span.ServiceName = "TestService";
            span.OperationName = "TestOperation";
            span.SetTag("k0", "v0");
            span.SetTag("k1", "v1");
            span.SetTag("k2", "v2");

            return span;
        }
    }
}
