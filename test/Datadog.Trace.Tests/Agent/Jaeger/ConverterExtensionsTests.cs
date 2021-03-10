using System.Linq;
using Datadog.Trace.Agent.Jaeger;
using Datadog.Trace.TestHelpers.Factories;
using Datadog.Trace.Util;
using Moq;
using Xunit;

namespace Datadog.Trace.Tests.Agent.Jaeger
{
    public class ConverterExtensionsTests
    {
        [Fact]
        public void JaegerTraceExporter_SetResource_CreatesTags()
        {
            var parentSpanContext = Mock.Of<ISpanContext>(c => c.SpanId == SpanIdGenerator.ThreadInstance.CreateNew());
            var span = SpanFactory.CreateSpan(parentSpanContext);

            JaegerSpan jaegerSpan = span.ToJaegerSpan();

            Assert.Equal("TestOperation", jaegerSpan.OperationName);

            Assert.Equal(default, jaegerSpan.TraceIdHigh); // Change when implementing W3C
            Assert.Equal((long)span.TraceId, jaegerSpan.TraceIdLow);
            Assert.Equal((long)span.SpanId, jaegerSpan.SpanId);
            Assert.Equal((long)span.Context.ParentId, jaegerSpan.ParentSpanId);

            Assert.Equal(0x1, jaegerSpan.Flags);

            Assert.Equal(span.StartTime.UtcDateTime.ToEpochMicroseconds(), jaegerSpan.StartTime);
            Assert.Equal((long)(span.Duration.TotalMilliseconds * 1000), jaegerSpan.Duration);

            var tags = jaegerSpan.Tags.ToArray();

            Assert.Equal(4, tags.Length);
            Assert.All(tags, t => Assert.Equal(JaegerTagType.STRING, t.VType));
            Assert.Single(tags, t => t.Key == "k0" && t.VStr == "v0");
            Assert.Single(tags, t => t.Key == "k1" && t.VStr == "v1");
            Assert.Single(tags, t => t.Key == Tags.Env && t.VStr == "Test");
            Assert.Single(tags, t => t.Key == Tags.Version && t.VStr == "v1.0");
        }
    }
}
