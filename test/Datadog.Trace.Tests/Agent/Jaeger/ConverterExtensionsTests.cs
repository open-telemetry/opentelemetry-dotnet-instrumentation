using System;
using System.Linq;
using Datadog.Trace.Agent.Jaeger;
using Datadog.Trace.Tagging;
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
            var parentSpanContext = new Mock<ISpanContext>();
            var traceContext = new Mock<ITraceContext>();
            var spanContext = new SpanContext(parentSpanContext.Object, traceContext.Object, serviceName: null);

            parentSpanContext.Setup(x => x.SpanId).Returns(SpanIdGenerator.ThreadInstance.CreateNew());

            var additionalTags = new CommonTags();
            additionalTags.Version = "v1.0";
            additionalTags.Environment = "Test";

            var start = DateTimeOffset.UtcNow.AddSeconds(-1.5);
            var span = new Span(spanContext, start, additionalTags);
            span.ServiceName = "ServiceName";
            span.OperationName = "TestOperation";
            span.SetTag("k0", "v0");
            span.SetTag("k1", "v1");

            span.Finish(TimeSpan.FromSeconds(1.5));

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
            var tag = tags[0];
            Assert.Equal(JaegerTagType.STRING, tag.VType);
            Assert.Equal("k0", tag.Key);
            Assert.Equal("v0", tag.VStr);
            tag = tags[1];
            Assert.Equal(JaegerTagType.STRING, tag.VType);
            Assert.Equal("k1", tag.Key);
            Assert.Equal("v1", tag.VStr);
            tag = tags[2];
            Assert.Equal(JaegerTagType.STRING, tag.VType);
            Assert.Equal(Tags.Env, tag.Key);
            Assert.Equal("Test", tag.VStr);
            tag = tags[3];
            Assert.Equal(JaegerTagType.STRING, tag.VType);
            Assert.Equal(Tags.Version, tag.Key);
            Assert.Equal("v1.0", tag.VStr);
        }
    }
}
