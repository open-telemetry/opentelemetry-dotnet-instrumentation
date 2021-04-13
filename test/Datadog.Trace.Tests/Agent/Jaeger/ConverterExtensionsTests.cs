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
        public void ToJaegerSpan()
        {
            var parentSpanContext = Mock.Of<ISpanContext>(c => c.SpanId == SpanIdGenerator.ThreadInstance.CreateNew());
            var span = SpanFactory.CreateSpan(parentSpanContext);

            JaegerSpan jaegerSpan = span.ToJaegerSpan();

            Assert.Equal("TestOperation", jaegerSpan.OperationName);

            Assert.Equal(span.TraceId.Higher, jaegerSpan.TraceIdHigh);
            Assert.Equal(span.TraceId.Lower, jaegerSpan.TraceIdLow);
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

        [Fact]
        public void ToJaegerTags_StatusUnset()
        {
            var span = SpanFactory.CreateSpan();

            var tags = span.ToJaegerTags();

            Assert.DoesNotContain(tags, t => t.Key == "otel.status_code"); // MUST NOT be set if the status code is UNSET.
            Assert.DoesNotContain(tags, t => t.Key == "otel.status_description"); // MUST NOT be set if the status code is UNSET.
        }

        [Fact]
        public void ToJaegerTags_StatusOk()
        {
            var span = SpanFactory.CreateSpan();
            span.Status = SpanStatus.Ok;

            var tags = span.ToJaegerTags();

            Assert.Contains(tags, t => t.Key == "otel.status_code" && t.VStr == "OK");
            Assert.DoesNotContain(tags, t => t.Key == "otel.status_description"); // MUST NOT be set if the status code is OK.
        }

        [Fact]
        public void ToJaegerTags_StatusError()
        {
            var span = SpanFactory.CreateSpan();
            span.Status = SpanStatus.Error;

            var tags = span.ToJaegerTags();

            Assert.Contains(tags, t => t.Key == "otel.status_code" && t.VStr == "ERROR");
            Assert.Contains(tags, t => t.Key == "otel.status_description" && t.VStr == string.Empty); // MUST be set if the code is ERROR, use an empty string if Description has no value.
            Assert.Contains(tags, t => t.Key == "error" && t.VBool.Value);
        }

        [Fact]
        public void ToJaegerTags_StatusErrorWithDescription()
        {
            const string errorDescription = "some error";
            var span = SpanFactory.CreateSpan();
            span.Status = SpanStatus.Error.WithDescription(errorDescription);

            var tags = span.ToJaegerTags();

            Assert.Contains(tags, t => t.Key == "otel.status_code" && t.VStr == "ERROR");
            Assert.Contains(tags, t => t.Key == "otel.status_description" && t.VStr == errorDescription);
            Assert.Contains(tags, t => t.Key == "error" && t.VBool.Value);
        }
    }
}
