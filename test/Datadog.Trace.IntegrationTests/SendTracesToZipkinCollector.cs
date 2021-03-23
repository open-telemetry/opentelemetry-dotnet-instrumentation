using System;
using Datadog.Trace.Agent;
using Datadog.Trace.Agent.Zipkin;
using Datadog.Trace.Configuration;
using Datadog.Trace.TestHelpers;
using Xunit;

namespace Datadog.Trace.IntegrationTests
{
    public class SendTracesToZipkinCollector
    {
        private readonly Tracer _tracer;
        private readonly int collectorPort = 9411;

        public SendTracesToZipkinCollector()
        {
            var agentUri = new Uri("http://localhost:9411/api/v2/spans");
            var exporter = new ZipkinExporter(agentUri);
            var exporterWriter = new ExporterWriter(exporter, new NullMetrics());
            _tracer = new Tracer(new TracerSettings(), exporterWriter, sampler: null, scopeManager: null, statsd: null);
        }

        [Fact]
        public void MinimalSpan()
        {
            using (var zipkinCollector = new MockZipkinCollector(collectorPort))
            {
                var scope = _tracer.StartActive("Operation");
                scope.Span.SetTag(Tags.SpanKind, SpanKinds.Client);
                scope.Span.SetTag("key", "value");
                scope.Dispose();

                var spans = zipkinCollector.WaitForSpans(1);

                Assert.Single(spans);
                var zspan = spans[0];
                Assert.Equal(scope.Span.OperationName, zspan.Name);
                Assert.True(zspan.Tags.TryGetValue("key", out var tagValue));
                Assert.Equal("value", tagValue);

                // The span.kind has an special treatment.
                Assert.False(zspan.Tags.ContainsKey(Tags.SpanKind));
                Assert.Equal("CLIENT", zspan.Kind);
            }
        }
    }
}
