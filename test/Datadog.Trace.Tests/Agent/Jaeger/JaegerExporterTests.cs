using Datadog.Trace.Agent.Jaeger;
using Datadog.Trace.TestHelpers.Factories;
using Moq;
using Xunit;

namespace Datadog.Trace.Tests.Agent.Jaeger
{
    public class JaegerExporterTests
    {
        [Fact]
        public void JaegerTraceExporter_ctor_NullServiceNameAllowed()
        {
            var jaegerTraceExporter = BuildExporter();
            Assert.NotNull(jaegerTraceExporter);
        }

        [Fact]
        public void JaegerTraceExporter_SetResource_UpdatesServiceName()
        {
            var jaegerTraceExporter = BuildExporter();
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
            var jaegerExporter = BuildExporter(175);
            jaegerExporter.SetResourceAndInitializeBatch("TestService");

            // Act
            jaegerExporter.AppendSpan(CreateTestJaegerSpan());
            jaegerExporter.AppendSpan(CreateTestJaegerSpan());
            jaegerExporter.AppendSpan(CreateTestJaegerSpan());

            // Assert
            Assert.Equal(1, jaegerExporter.Batch.Count);
        }

        internal static JaegerExporter BuildExporter(int? payloadSize = null)
        {
            var clientMock = new Mock<IJaegerClient>();

            clientMock
                .Setup(x => x.Send(It.IsAny<byte[]>()))
                .Returns<byte[]>((buffer) => buffer?.Length ?? 0);
            clientMock
                .Setup(x => x.Send(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((buffer, offset, count) => count);

            var options = new JaegerOptions()
            {
                TransportClient = clientMock.Object
            };

            if (payloadSize.HasValue)
            {
                options.MaxPayloadSizeInBytes = payloadSize.Value;
            }

            return new JaegerExporter(options);
        }

        internal static JaegerSpan CreateTestJaegerSpan()
        {
            return SpanFactory
                .CreateSpan()
                .ToJaegerSpan();
        }
    }
}
