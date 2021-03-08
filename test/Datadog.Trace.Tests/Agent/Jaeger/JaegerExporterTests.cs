using System;
using Datadog.Trace.Agent.Jaeger;
using Xunit;

namespace Datadog.Trace.Tests.Agent.Jaeger
{
    public class JaegerExporterTests
    {
        private static readonly Uri _exporterUri = new("udp://localhost:6831");

        [Fact]
        public void JaegerTraceExporter_ctor_NullServiceNameAllowed()
        {
            var jaegerTraceExporter = new JaegerExporter(_exporterUri, null);
            Assert.NotNull(jaegerTraceExporter);
        }

        [Fact]
        public void JaegerTraceExporter_SetResource_UpdatesServiceName()
        {
            var jaegerTraceExporter = new JaegerExporter(_exporterUri, null);
            var process = jaegerTraceExporter.Process;

            process.ServiceName = "TestService";

            jaegerTraceExporter.SetResourceAndInitializeBatch("TestService");

            Assert.Equal("TestService", process.ServiceName);

            jaegerTraceExporter.SetResourceAndInitializeBatch("MyService");

            Assert.Equal("MyService", process.ServiceName);

            jaegerTraceExporter.SetResourceAndInitializeBatch("MyService", "MyNamespace");

            Assert.Equal("MyNamespace.MyService", process.ServiceName);
        }

        [Fact]
        public void JaegerTraceExporter_BuildBatchesToTransmit_FlushedBatch()
        {
            // Arrange
            var jaegerExporter = new JaegerExporter(_exporterUri, null, 125);
            jaegerExporter.SetResourceAndInitializeBatch("TestService");

            // Act
            jaegerExporter.AppendSpan(CreateTestJaegerSpan());
            jaegerExporter.AppendSpan(CreateTestJaegerSpan());
            jaegerExporter.AppendSpan(CreateTestJaegerSpan());

            // Assert
            Assert.Equal(1, jaegerExporter.Batch.Count);
        }

        internal static JaegerSpan CreateTestJaegerSpan()
        {
            return SpanFactory
                .CreateSpan()
                .ToJaegerSpan();
        }
    }
}
