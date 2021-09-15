using System;
using Datadog.Trace.Tagging;
using Moq;

namespace Datadog.Trace.TestHelpers.Factories
{
    public static class SpanFactory
    {
        public static Span CreateSpan()
        {
            return CreateSpan(Mock.Of<ISpanContext>());
        }

        public static Span CreateSpan(ISpanContext parentContext)
        {
            var spanContext = new SpanContext(parentContext, Mock.Of<ITraceContext>(), serviceName: null);

            var additionalTags = new CommonTags()
            {
                Version = "v1.0",
                Environment = "Test"
            };

            var start = DateTimeOffset.UtcNow.AddSeconds(-1.5);
            var span = new Span(spanContext, start, additionalTags);
            span.ServiceName = "TestService";
            span.OperationName = "TestOperation";
            span.SetTag("k0", "v0");
            span.SetTag("k1", "v1");
            span.Finish(TimeSpan.FromSeconds(1.5));

            return span;
        }
    }
}
