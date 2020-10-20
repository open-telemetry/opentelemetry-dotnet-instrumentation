using System;
using System.Linq;
using System.Threading.Tasks;
using Datadog.Trace.Agent;
using Moq;
using Xunit;

namespace Datadog.Trace.Tests
{
    public class AgentWriterTests
    {
        private readonly AgentWriter _agentWriter;
        private readonly Mock<IApi> _api;
        private readonly SpanContext _spanContext;

        public AgentWriterTests()
        {
            var tracer = new Mock<IDatadogTracer>();
            tracer.Setup(x => x.DefaultServiceName).Returns("Default");

            _api = new Mock<IApi>();
            _agentWriter = new AgentWriter(_api.Object, statsd: null);

            var parentSpanContext = new Mock<ISpanContext>();
            var traceContext = new Mock<ITraceContext>();
            _spanContext = new SpanContext(parentSpanContext.Object, traceContext.Object, serviceName: null);
        }

        [Fact]
        public async Task WriteTrace_2Traces_SendToApi()
        {
            // TODO:bertrand it is too complicated to setup such a simple test
            var trace = new[] { new Span(_spanContext, start: null) };
            _agentWriter.WriteTrace(trace);
            await _agentWriter.FlushTracesAsync(); // Force a flush to make sure the trace is written to the API
            _api.Verify(x => x.SendTracesAsync(It.Is<Span[][]>(y => y.Single().Equals(trace))), Times.Once);

            trace = new[] { new Span(_spanContext, start: null) };
            _agentWriter.WriteTrace(trace);
            await _agentWriter.FlushTracesAsync(); // Force a flush to make sure the trace is written to the API
            _api.Verify(x => x.SendTracesAsync(It.Is<Span[][]>(y => y.Single().Equals(trace))), Times.Once);
        }

        [Fact]
        public async Task FlushTwice()
        {
            var w = new AgentWriter(_api.Object, statsd: null);
            await w.FlushAndCloseAsync();
            await w.FlushAndCloseAsync();
        }
    }
}
