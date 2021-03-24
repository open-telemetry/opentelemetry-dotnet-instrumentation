using System.Globalization;
using Datadog.Trace.Headers;
using Datadog.Trace.Propagation;
using Datadog.Trace.TestHelpers;
using Xunit;

namespace Datadog.Trace.Tests.Headers
{
    // TODO: for now, these tests cover all of this,
    // but we should probably split them up into actual *unit* tests for:
    // - HttpHeadersCollection wrapper over HttpHeaders (Get, Set, Add, Remove)
    // - NameValueHeadersCollection wrapper over NameValueCollection (Get, Set, Add, Remove)
    // - B3SpanContextPropagator.Inject()
    // - B3SpanContextPropagator.Extract()
    public class B3HeadersCollectionTests : HeadersCollectionTestExtensions
    {
        private readonly B3SpanContextPropagator _propagator = new B3SpanContextPropagator();

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void HttpRequestMessage_InjectExtract_Identity(IHeadersCollection headers)
        {
            const int traceId = 9;
            const int spanId = 7;
            const SamplingPriority samplingPriority = SamplingPriority.UserKeep;

            var context = new SpanContext(traceId, spanId, samplingPriority);

            _propagator.Inject(context, headers);
            var resultContext = _propagator.Extract(headers);

            Assert.NotNull(resultContext);
            Assert.Equal(context.SpanId, resultContext.SpanId);
            Assert.Equal(context.TraceId, resultContext.TraceId);
            Assert.Equal(context.SamplingPriority, resultContext.SamplingPriority);
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void WebRequest_InjectExtract_Identity(IHeadersCollection headers)
        {
            const int traceId = 9;
            const int spanId = 7;
            const SamplingPriority samplingPriority = SamplingPriority.UserKeep;

            var context = new SpanContext(traceId, spanId, samplingPriority);

            _propagator.Inject(context, headers);
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
            const ulong traceId = 9;
            const SamplingPriority samplingPriority = SamplingPriority.UserKeep;

            InjectContext(
                headers,
                traceId.ToString(CultureInfo.InvariantCulture),
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
            const ulong traceId = 9;
            const ulong spanId = 7;

            InjectContext(
                headers,
                traceId.ToString("x16", CultureInfo.InvariantCulture),
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
