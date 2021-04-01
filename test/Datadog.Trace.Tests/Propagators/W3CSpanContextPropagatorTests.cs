using System.Collections.Generic;
using Datadog.Trace.Conventions;
using Datadog.Trace.Headers;
using Datadog.Trace.Propagation;
using Datadog.Trace.TestHelpers;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Datadog.Trace.Tests.Propagators
{
    public class W3CSpanContextPropagatorTests : HeadersCollectionTestBase
    {
        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void Inject_CratesCorrectHeader(IHeadersCollection headers)
        {
            var traceId = TraceId.CreateFromString("0af7651916cd43dd8448eb211c80319c");
            const ulong spanId = 67667974448284343;
            var spanContext = new SpanContext(traceId, spanId);
            var propagator = new W3CSpanContextPropagator(new OtelTraceIdConvention());

            propagator.Inject(spanContext, headers);

            using (new AssertionScope())
            {
                headers.GetValues(W3CHeaderNames.TraceParent).Should().HaveCount(1);
                headers.GetValues(W3CHeaderNames.TraceParent).Should().BeEquivalentTo(new List<string> { "00-0af7651916cd43dd8448eb211c80319c-00f067aa0ba902b7-01" });
            }
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void Extract_ReturnsCorrectContext(IHeadersCollection headers)
        {
            var propagator = new W3CSpanContextPropagator(new OtelTraceIdConvention());
            headers.Set(W3CHeaderNames.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-00f067aa0ba902b7-01");

            var spanContext = propagator.Extract(headers);

            using (new AssertionScope())
            {
                spanContext.SpanId.Should().Be(67667974448284343);
                spanContext.TraceId.Should().Be(TraceId.CreateFromString("0af7651916cd43dd8448eb211c80319c"));
            }
        }
    }
}
