using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using Datadog.Trace.ClrProfiler.Helpers;
using Datadog.Trace.Conventions;
using Datadog.Trace.Headers;
using Datadog.Trace.Propagation;
using Datadog.Trace.TestHelpers;
using Xunit;

namespace Datadog.Trace.ClrProfiler.Managed.Tests
{
    public class ReflectionHttpHeadersCollectionTests
    {
        private readonly DDSpanContextPropagator _propagator = new DDSpanContextPropagator(new DatadogTraceIdConvention());

        public static IEnumerable<object[]> GetInvalidIds() => HeadersCollectionTestHelpers.GetInvalidIds();

        public static IEnumerable<object[]> GetInvalidSamplingPriorities() => HeadersCollectionTestHelpers.GetInvalidSamplingPriorities();

        [Fact]
        public void ExtractHeaderTags_EmptyHeadersReturnsEmptyTagsList()
        {
            // HttpRequestHeaders setup
            var request = new HttpRequestMessage();
            var headers = new ReflectionHttpHeadersCollection(request.Headers);

            var tagsFromHeader = headers.ExtractHeaderTags(new Dictionary<string, string>());

            Assert.NotNull(tagsFromHeader);
            Assert.Empty(tagsFromHeader);
        }

        [Fact]
        public void ExtractHeaderTags_MatchesCaseInsensitive()
        {
            // HttpRequestHeaders setup
            var request = new HttpRequestMessage();
            var headers = new ReflectionHttpHeadersCollection(request.Headers);

            // Initialize constants
            const string customHeader1Name = "dd-custom-header1";
            const string customHeader1Value = "match1";
            const string customHeader1TagName = "custom-header1-tag";

            const string customHeader2Name = "DD-CUSTOM-HEADER-MISMATCHING-CASE";
            const string customHeader2Value = "match2";
            const string customHeader2TagName = "custom-header2-tag";
            string customHeader2LowercaseHeaderName = customHeader2Name.ToLowerInvariant();

            // Initialize WebRequest and add headers
            headers.Add(customHeader1Name, customHeader1Value);
            headers.Add(customHeader2Name, customHeader2Value);

            // Initialize header tag arguments
            var headerToTagMap = new Dictionary<string, string>();
            headerToTagMap.Add(customHeader1Name, customHeader1TagName);
            headerToTagMap.Add(customHeader2LowercaseHeaderName, customHeader2TagName);

            // Set expectations
            var expectedResults = new Dictionary<string, string>();
            expectedResults.Add(customHeader1TagName, customHeader1Value);
            expectedResults.Add(customHeader2TagName, customHeader2Value);

            // Test
            var tagsFromHeader = headers.ExtractHeaderTags(headerToTagMap);

            // Assert
            Assert.NotNull(tagsFromHeader);
            Assert.Equal(expectedResults, tagsFromHeader);
        }

        [Fact]
        public void Extract_EmptyHeadersReturnsNull()
        {
            // HttpRequestHeaders setup
            var request = new HttpRequestMessage();
            var headers = new ReflectionHttpHeadersCollection(request.Headers);

            var resultContext = _propagator.Extract(headers);
            Assert.Null(resultContext);
        }

        [Fact]
        public void InjectExtract_Identity()
        {
            // HttpRequestHeaders setup
            var request = new HttpRequestMessage();
            var headers = new ReflectionHttpHeadersCollection(request.Headers);

            var traceId = TraceId.CreateFromInt(9);
            const int spanId = 7;
            const SamplingPriority samplingPriority = SamplingPriority.UserKeep;
            const string origin = "synthetics";

            var context = new SpanContext(traceId, spanId, samplingPriority, null, origin);
            _propagator.Inject(context, headers);
            var resultContext = _propagator.Extract(headers);

            Assert.NotNull(resultContext);
            Assert.Equal(context.SpanId, resultContext.SpanId);
            Assert.Equal(context.TraceId, resultContext.TraceId);
            Assert.Equal(context.SamplingPriority, resultContext.SamplingPriority);
            Assert.Equal(context.Origin, resultContext.Origin);
        }

        [Theory]
        [MemberData(nameof(GetInvalidIds))]
        public void Extract_InvalidTraceId(string traceId)
        {
            // HttpRequestHeaders setup
            var request = new HttpRequestMessage();
            var headers = new ReflectionHttpHeadersCollection(request.Headers);

            const string spanId = "7";
            const string samplingPriority = "2";
            const string origin = "synthetics";

            InjectContext(headers, traceId, spanId, samplingPriority, origin);
            var resultContext = _propagator.Extract(headers);

            // invalid traceId should return a null context even if other values are set
            Assert.Null(resultContext);
        }

        [Theory]
        [MemberData(nameof(GetInvalidIds))]
        public void Extract_InvalidSpanId(string spanId)
        {
            // HttpRequestHeaders setup
            var request = new HttpRequestMessage();
            var headers = new ReflectionHttpHeadersCollection(request.Headers);

            var traceId = TraceId.CreateFromInt(9);
            const SamplingPriority samplingPriority = SamplingPriority.UserKeep;
            const string origin = "synthetics";

            InjectContext(
                headers,
                traceId.ToString(),
                spanId,
                ((int)samplingPriority).ToString(CultureInfo.InvariantCulture),
                origin);

            var resultContext = _propagator.Extract(headers);

            Assert.NotNull(resultContext);
            Assert.Equal(traceId, resultContext.TraceId);
            Assert.Equal(default(ulong), resultContext.SpanId);
            Assert.Equal(samplingPriority, resultContext.SamplingPriority);
            Assert.Equal(origin, resultContext.Origin);
        }

        [Theory]
        [MemberData(nameof(GetInvalidSamplingPriorities))]
        public void Extract_InvalidSamplingPriority(string samplingPriority)
        {
            // HttpRequestHeaders setup
            var request = new HttpRequestMessage();
            var headers = new ReflectionHttpHeadersCollection(request.Headers);

            var traceId = TraceId.CreateFromInt(9);
            const ulong spanId = 7;
            const string origin = "synthetics";

            InjectContext(
                headers,
                traceId.ToString(),
                spanId.ToString(CultureInfo.InvariantCulture),
                samplingPriority,
                origin);

            var resultContext = _propagator.Extract(headers);

            Assert.NotNull(resultContext);
            Assert.Equal(traceId, resultContext.TraceId);
            Assert.Equal(spanId, resultContext.SpanId);
            Assert.Null(resultContext.SamplingPriority);
            Assert.Equal(origin, resultContext.Origin);
        }

        private static void InjectContext(IHeadersCollection headers, string traceId, string spanId, string samplingPriority, string origin)
        {
            headers.Add(DDHttpHeaderNames.TraceId, traceId);
            headers.Add(DDHttpHeaderNames.ParentId, spanId);
            headers.Add(DDHttpHeaderNames.SamplingPriority, samplingPriority);
            headers.Add(DDHttpHeaderNames.Origin, origin);
        }
    }
}
