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
        internal void Inject_CratesCorrectTraceParentHeader(IHeadersCollection headers)
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
        internal void Inject_CreateCorrectTraceStateHeaderIfPresent(IHeadersCollection headers)
        {
            var traceId = TraceId.CreateFromString("0af7651916cd43dd8448eb211c80319c");
            const ulong spanId = 67667974448284343;
            var spanContext = new SpanContext(traceId, spanId, "state");
            var propagator = new W3CSpanContextPropagator(new OtelTraceIdConvention());

            propagator.Inject(spanContext, headers);

            using (new AssertionScope())
            {
                headers.GetValues(W3CHeaderNames.TraceState).Should().HaveCount(1);
                headers.GetValues(W3CHeaderNames.TraceState).Should().BeEquivalentTo(new List<string> { "state" });
            }
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void Inject_DoNotCreateCorrectTraceStateHeaderIfNotPresent(IHeadersCollection headers)
        {
            var traceId = TraceId.CreateFromString("0af7651916cd43dd8448eb211c80319c");
            const ulong spanId = 67667974448284343;
            var spanContext = new SpanContext(traceId, spanId, traceState: null);
            var propagator = new W3CSpanContextPropagator(new OtelTraceIdConvention());

            propagator.Inject(spanContext, headers);

            headers.GetValues(W3CHeaderNames.TraceState).Should().HaveCount(0);
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void Extract_ReturnCorrectTraceAndSpanIdInContext(IHeadersCollection headers)
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

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void Extract_ReturnCorrectTraceStateInContextIfPresent(IHeadersCollection headers)
        {
            var propagator = new W3CSpanContextPropagator(new OtelTraceIdConvention());
            headers.Set(W3CHeaderNames.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-00f067aa0ba902b7-01");
            headers.Set(W3CHeaderNames.TraceState, "state");

            var spanContext = propagator.Extract(headers);

            spanContext.TraceState.Should().Be("state");
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void Extract_DoNotReturnTraceStateInContextIfNotPresent(IHeadersCollection headers)
        {
            var propagator = new W3CSpanContextPropagator(new OtelTraceIdConvention());
            headers.Set(W3CHeaderNames.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-00f067aa0ba902b7-01");

            var spanContext = propagator.Extract(headers);

            spanContext.TraceState.Should().BeNullOrEmpty();
        }
    }
}
