using System;
using System.Linq;
using System.Threading.Tasks;
using Datadog.Trace.Agent;
using Moq;
using Xunit;

namespace Datadog.Trace.Tests
{
    public class ExporterWriterTests
    {
        private readonly ExporterWriter _exporterWriter;
        private readonly Mock<IExporter> _exporter;
        private readonly SpanContext _spanContext;

        public ExporterWriterTests()
        {
            var tracer = new Mock<IDatadogTracer>();
            tracer.Setup(x => x.DefaultServiceName).Returns("Default");

            _exporter = new Mock<IExporter>();
            _exporterWriter = new ExporterWriter(_exporter.Object, new NullMetrics());

            var parentSpanContext = new Mock<ISpanContext>();
            var traceContext = new Mock<ITraceContext>();
            _spanContext = new SpanContext(parentSpanContext.Object, traceContext.Object, serviceName: null);
        }

        [Fact]
        public async Task WriteTrace_2Traces_SendToApi()
        {
            // TODO:bertrand it is too complicated to setup such a simple test
            var trace = new ArraySegment<Span>(new[] { new Span(_spanContext, start: null) });
            _exporterWriter.WriteTrace(trace);
            await _exporterWriter.FlushTracesAsync(); // Force a flush to make sure the trace is written to the API
            _exporter.Verify(x => x.SendTracesAsync(It.Is<Span[][]>(y => y.Single().Equals(trace))), Times.Once);

            trace = new ArraySegment<Span>(new[] { new Span(_spanContext, start: null) });
            _exporterWriter.WriteTrace(trace);
            await _exporterWriter.FlushTracesAsync(); // Force a flush to make sure the trace is written to the API
            _exporter.Verify(x => x.SendTracesAsync(It.Is<Span[][]>(y => y.Single().Equals(trace))), Times.Once);
        }

        [Fact]
        public async Task FlushTwice()
        {
            var w = new ExporterWriter(_exporter.Object, new NullMetrics());
            await w.FlushAndCloseAsync();
            await w.FlushAndCloseAsync();
        }
    }
}
