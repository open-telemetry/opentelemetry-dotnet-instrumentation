using System.Collections.Generic;
using System.IO;
using Datadog.Trace.Agent;
using Datadog.Trace.Tagging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Datadog.Trace.Tests
{
    public class ZipkinSerializerTests
    {
        [Fact]
        public void RoundTripRootSpanTest()
        {
            var parentSpanContext = new Mock<ISpanContext>();
            var traceContext = new Mock<ITraceContext>();
            var spanContext = new SpanContext(parentSpanContext.Object, traceContext.Object, serviceName: null);

            var additionalTags = new HttpTags();

            additionalTags.HttpMethod = "GET";
            additionalTags.HttpStatusCode = "200";
            additionalTags.HttpUrl = "Sample/Test";

            var span = new Span(spanContext, start: null, tags: additionalTags);
            span.ServiceName = "ServiceName";
            span.SetTag("k0", "v0");
            span.SetTag("k1", "v1");
            span.SetTag("k2", "v2");

            var serializerSpan = new ZipkinSerializer.ZipkinSpan(span);

            var serializer = new ZipkinSerializer();
            using var ms = new MemoryStream();

            serializer.Serialize(ms, new[] { new[] { span } });

            ms.Position = 0;
            var jsonText = new StreamReader(ms).ReadToEnd();
            var actualTraces = JsonConvert.DeserializeObject<List<TestZipkinSpan>>(jsonText);

            Assert.Single(actualTraces);

            var actualSpan = actualTraces[0];
            actualSpan.AssertZipkinSerializerSpan(serializerSpan);
        }

        public class TestZipkinSpan
        {
            public string Id { get; set; }

            public string TraceId { get; set; }

            public string ParentId { get; set; }

            public string Name { get; set; }

            public long Timestamp { get; set; }

            public long Duration { get; set; }

            public string Kind { get; set; }

            public Dictionary<string, string> LocalEndpoint { get; set; }

            public IDictionary<string, string> Tags { get; set; }

            internal void AssertZipkinSerializerSpan(ZipkinSerializer.ZipkinSpan span)
            {
                Assert.Equal(Id, span.Id);
                Assert.Equal(TraceId, span.TraceId);
                Assert.Equal(ParentId, span.ParentId);
                Assert.Equal(Name, span.Name);
                Assert.Equal(Timestamp, span.Timestamp);
                Assert.Equal(Duration, span.Duration);
                Assert.Equal(Kind, span.Kind);
                Assert.Equal(LocalEndpoint.Count, span.LocalEndpoint.Count);
                Assert.Contains(
                    LocalEndpoint,
                    kvp => span.LocalEndpoint.TryGetValue(kvp.Key, out var value) && kvp.Value == value);
                Assert.Equal(Tags.Count, span.Tags.Count);
                Assert.Contains(
                    Tags,
                    kvp => span.Tags.TryGetValue(kvp.Key, out var value) && kvp.Value == value);
            }
        }
    }
}
