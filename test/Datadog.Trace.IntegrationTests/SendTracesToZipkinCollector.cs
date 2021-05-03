using System;
using Datadog.Trace.Agent;
using Datadog.Trace.Agent.Zipkin;
using Datadog.Trace.Configuration;
using Datadog.Trace.TestHelpers;
using Xunit;

namespace Datadog.Trace.IntegrationTests
{
    public class SendTracesToZipkinCollector : IDisposable
    {
        private readonly Tracer _tracer;
        private readonly MockZipkinCollector _zipkinCollector;

        public SendTracesToZipkinCollector()
        {
            int collectorPort = 9411;
            var agentUri = new Uri($"http://localhost:{collectorPort}/api/v2/spans");
            var exporter = new ZipkinExporter(agentUri);
            var exporterWriter = new ExporterWriter(exporter, new NullMetrics());
            _tracer = new Tracer(new TracerSettings(), plugins: null, exporterWriter, sampler: null, scopeManager: null, statsd: null);
            _zipkinCollector = new MockZipkinCollector(collectorPort);
        }

        public void Dispose()
        {
            _zipkinCollector.Dispose();
        }

        [Fact]
        public void MinimalSpan()
        {
            const string operationName = "MyOperation";
            using (var scope = _tracer.StartActive(operationName))
            {
                scope.Span.SetTag(Tags.SpanKind, SpanKinds.Client);
                scope.Span.SetTag("key", "value");
            }

            var zspan = RetrieveSpan();

            Assert.Equal(operationName, zspan.Name);
            Assert.True(zspan.Tags.TryGetValue("key", out var tagValue));
            Assert.Equal("value", tagValue);

            // The span.kind has an special treatment.
            Assert.False(zspan.Tags.ContainsKey(Tags.SpanKind));
            Assert.Equal("CLIENT", zspan.Kind);

            Assert.False(zspan.Tags.ContainsKey("otel.status_code")); // MUST NOT be set if the status code is UNSET.
            Assert.False(zspan.Tags.ContainsKey("error")); // MUST NOT be set if the status code is UNSET.
        }

        [Fact]
        public void OkSpan()
        {
            using (var scope = _tracer.StartActive("Operation"))
            {
                scope.Span.Status = SpanStatus.Ok;
            }

            var zspan = RetrieveSpan();

            Assert.True(zspan.Tags.TryGetValue("otel.status_code", out var tagValue));
            Assert.Equal("OK", tagValue);
            Assert.False(zspan.Tags.ContainsKey("error")); // MUST NOT be set if the status code is OK.
        }

        [Fact]
        public void ErrorSpan()
        {
            using (var scope = _tracer.StartActive("Operation"))
            {
                scope.Span.Status = SpanStatus.Error;
            }

            var zspan = RetrieveSpan();

            Assert.True(zspan.Tags.TryGetValue("otel.status_code", out var tagValue));
            Assert.Equal("ERROR", tagValue);
            Assert.True(zspan.Tags.TryGetValue("error", out var errorValue)); // MUST be set if the code is ERROR, use an empty string if Description has no value.
            Assert.Equal(string.Empty, errorValue);
        }

        [Fact]
        public void ErrorSpanWithDescription()
        {
            const string errorDescription = "some error";
            using (var scope = _tracer.StartActive("Operation"))
            {
                scope.Span.Status = SpanStatus.Error.WithDescription(errorDescription);
            }

            var zspan = RetrieveSpan();

            Assert.True(zspan.Tags.TryGetValue("otel.status_code", out var tagValue));
            Assert.Equal("ERROR", tagValue);
            Assert.True(zspan.Tags.TryGetValue("error", out var errorValue));
            Assert.Equal(errorDescription, errorValue);
        }

        private MockZipkinCollector.Span RetrieveSpan()
        {
            var spans = _zipkinCollector.WaitForSpans(1);
            Assert.Single(spans);
            return spans[0];
        }
    }
}
