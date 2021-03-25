using System.Globalization;
using Datadog.Trace.Conventions;
using Datadog.Trace.Headers;
using Datadog.Trace.Propagation;
using Datadog.Trace.TestHelpers;
using Xunit;

namespace Datadog.Trace.Tests.Propagators
{
    public class B3SpanContextPropagatorTests : HeadersCollectionTestBase
    {
        private B3SpanContextPropagator _propagator;

        public B3SpanContextPropagatorTests()
        {
            _propagator = new B3SpanContextPropagator(new OtelTraceIdConvention());
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void HttpRequestMessage_InjectExtract_Identity(IHeadersCollection headers)
        {
            var traceId = TraceId.CreateFromString("52686470458518446744073709551615");
            const ulong spanId = 18446744073709551614;
            const SamplingPriority samplingPriority = SamplingPriority.AutoKeep;

            var context = new SpanContext(traceId, spanId, samplingPriority);

            _propagator.Inject(context, headers);

            AssertExpected(headers, B3HttpHeaderNames.B3TraceId, "52686470458518446744073709551615");
            AssertExpected(headers, B3HttpHeaderNames.B3SpanId, "fffffffffffffffe");
            AssertExpected(headers, B3HttpHeaderNames.B3Sampled, "1");
            AssertMissing(headers, B3HttpHeaderNames.B3ParentId);
            AssertMissing(headers, B3HttpHeaderNames.B3Flags);

            var resultContext = _propagator.Extract(headers);

            Assert.NotNull(resultContext);
            Assert.Equal(context.SpanId, resultContext.SpanId);
            Assert.Equal(context.TraceId, resultContext.TraceId);
            Assert.Equal(context.SamplingPriority, resultContext.SamplingPriority);
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void HttpRequestMessage_InjectExtract_Identity_WithParent(IHeadersCollection headers)
        {
            var traceId = TraceId.CreateFromString("52686470458518446744073709551615");
            const ulong spanId = 18446744073709551614;
            const SamplingPriority samplingPriority = SamplingPriority.UserKeep;

            var parentContext = new SpanContext(traceId, spanId, samplingPriority);
            var traceContext = new TraceContext(null)
            {
                SamplingPriority = samplingPriority
            };

            var context = new SpanContext(parentContext, traceContext, null);

            _propagator.Inject(context, headers);

            AssertExpected(headers, B3HttpHeaderNames.B3TraceId, "52686470458518446744073709551615");
            AssertExpected(headers, B3HttpHeaderNames.B3SpanId, context.SpanId.ToString("x16"));
            AssertExpected(headers, B3HttpHeaderNames.B3ParentId, "fffffffffffffffe");
            AssertExpected(headers, B3HttpHeaderNames.B3Flags, "1");
            AssertMissing(headers, B3HttpHeaderNames.B3Sampled);

            var resultContext = _propagator.Extract(headers);

            Assert.NotNull(resultContext);
            Assert.Equal(context.SpanId, resultContext.SpanId);
            Assert.Equal(context.TraceId, resultContext.TraceId);
            Assert.Equal(samplingPriority, resultContext.SamplingPriority);
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void WebRequest_InjectExtract_Identity(IHeadersCollection headers)
        {
            var traceId = TraceId.CreateFromString("52686470458518446744073709551615");
            const int spanId = 2147483646;
            const SamplingPriority samplingPriority = SamplingPriority.AutoReject;

            var context = new SpanContext(traceId, spanId, samplingPriority);

            _propagator.Inject(context, headers);

            AssertExpected(headers, B3HttpHeaderNames.B3TraceId, "52686470458518446744073709551615");
            AssertExpected(headers, B3HttpHeaderNames.B3SpanId, "000000007ffffffe");
            AssertExpected(headers, B3HttpHeaderNames.B3Sampled, "0");
            AssertMissing(headers, B3HttpHeaderNames.B3ParentId);
            AssertMissing(headers, B3HttpHeaderNames.B3Flags);

            var resultContext = _propagator.Extract(headers);

            Assert.NotNull(resultContext);
            Assert.Equal(context.SpanId, resultContext.SpanId);
            Assert.Equal(context.TraceId, resultContext.TraceId);
            Assert.Equal(context.SamplingPriority, resultContext.SamplingPriority);
        }

        [Theory]
        [MemberData(nameof(GetHeadersInvalidIdsCartesianProduct))]
        internal void Extract_InvalidTraceId(IHeadersCollection headers, string traceId)
        {
            const string spanId = "7";
            const string samplingPriority = "2";

            InjectContext(headers, traceId, spanId, samplingPriority);
            var resultContext = _propagator.Extract(headers);

            // invalid traceId should return a null context even if other values are set
            Assert.Null(resultContext);
        }

        [Theory]
        [MemberData(nameof(GetHeadersInvalidIdsCartesianProduct))]
        internal void Extract_InvalidSpanId(IHeadersCollection headers, string spanId)
        {
            var traceId = TraceId.CreateFromString("52686470458518446744073709551615");
            const SamplingPriority samplingPriority = SamplingPriority.UserKeep;

            InjectContext(
                headers,
                traceId.ToString(),
                spanId,
                ((int)samplingPriority).ToString(CultureInfo.InvariantCulture));
            var resultContext = _propagator.Extract(headers);

            Assert.NotNull(resultContext);
            Assert.Equal(traceId, resultContext.TraceId);
            Assert.Equal(default(ulong), resultContext.SpanId);
            Assert.Equal(samplingPriority, resultContext.SamplingPriority);
        }

        [Theory]
        [MemberData(nameof(GetHeadersInvalidSamplingPrioritiesCartesianProduct))]
        internal void Extract_InvalidSamplingPriority(IHeadersCollection headers, string samplingPriority)
        {
            var traceId = TraceId.CreateFromString("52686470458518446744073709551615");
            const ulong spanId = 23456789;

            InjectContext(
                headers,
                traceId.ToString(),
                spanId.ToString("x16", CultureInfo.InvariantCulture),
                samplingPriority);

            var resultContext = _propagator.Extract(headers);

            Assert.NotNull(resultContext);
            Assert.Equal(traceId, resultContext.TraceId);
            Assert.Equal(spanId, resultContext.SpanId);
            Assert.Null(resultContext.SamplingPriority);
        }

        private static IHeadersCollection InjectContext(IHeadersCollection headers, string traceId, string spanId, string samplingPriority)
        {
            headers.Add(B3HttpHeaderNames.B3TraceId, traceId);
            headers.Add(B3HttpHeaderNames.B3SpanId, spanId);

            // Mimick the B3 injection mapping of samplingPriority
            switch (samplingPriority)
            {
                case "-1":
                // SamplingPriority.UserReject
                case "0":
                    // SamplingPriority.AutoReject
                    headers.Add(B3HttpHeaderNames.B3Flags, "0");
                    break;
                case "1":
                    // SamplingPriority.AutoKeep
                    headers.Add(B3HttpHeaderNames.B3Sampled, "1");
                    break;
                case "2":
                    // SamplingPriority.UserKeep
                    headers.Add(B3HttpHeaderNames.B3Flags, "1");
                    break;
                default:
                    // Invalid samplingPriority
                    break;
            }

            return headers;
        }
    }
}
