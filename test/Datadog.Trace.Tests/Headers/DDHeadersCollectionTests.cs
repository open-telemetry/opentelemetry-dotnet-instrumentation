// <copyright file="HeadersCollectionTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;
using System.Globalization;
using Datadog.Trace.Conventions;
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
    // - SpanContextPropagator.Inject()
    // - SpanContextPropagator.Extract()
    public class DDHeadersCollectionTests : HeadersCollectionTestBase
    {
        private const string TestPrefix = "test.prefix";
        private readonly DDSpanContextPropagator _propagator = new DDSpanContextPropagator(new DatadogTraceIdConvention());

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void ExtractHeaderTags_EmptyHeadersReturnsEmptyTagsList(IHeadersCollection headers)
        {
            var tagsFromHeader = headers.ExtractHeaderTags(new Dictionary<string, string>(), TestPrefix);

            Assert.NotNull(tagsFromHeader);
            Assert.Empty(tagsFromHeader);
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void ExtractHeaderTags_MatchesCaseInsensitive(IHeadersCollection headers)
        {
            // Initialize constants
            const string customHeader1Name = "dd-custom-header1";
            const string customHeader1Value = "match1";
            const string customHeader1TagName = "custom-header1-tag";

            const string customHeader2Name = "DD-CUSTOM-HEADER-MISMATCHING-CASE";
            const string customHeader2Value = "match2";
            const string customHeader2TagName = "custom-header2-tag";
            string customHeader2LowercaseHeaderName = customHeader2Name.ToLowerInvariant();

            // Add headers
            headers.Add(customHeader1Name, customHeader1Value);
            headers.Add(customHeader2Name, customHeader2Value);

            // Initialize header-tag arguments and expectations
            var headerTags = new Dictionary<string, string>();
            headerTags.Add(customHeader1Name, customHeader1TagName);
            headerTags.Add(customHeader2LowercaseHeaderName, customHeader2TagName);

            var expectedResults = new Dictionary<string, string>();
            expectedResults.Add(customHeader1TagName, customHeader1Value);
            expectedResults.Add(customHeader2TagName, customHeader2Value);

            // Test
            var tagsFromHeader = headers.ExtractHeaderTags(headerTags, TestPrefix);

            // Assert
            Assert.NotNull(tagsFromHeader);
            Assert.Equal(expectedResults, tagsFromHeader);
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void ExtractHeaderTags_EmptyHeaders_AddsNoTags(IHeadersCollection headers)
        {
            // Do not add headers

            // Initialize header-tag arguments and expectations
            var headerTags = new Dictionary<string, string>();
            headerTags.Add("x-header-test-runner", "test-runner");

            var expectedResults = new Dictionary<string, string>();

            // Test
            var tagsFromHeader = headers.ExtractHeaderTags(headerTags, TestPrefix);

            // Assert
            Assert.NotNull(tagsFromHeader);
            Assert.Equal(expectedResults, tagsFromHeader);
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void ExtractHeaderTags_EmptyHeaderTags_AddsNoTags(IHeadersCollection headers)
        {
            // Add headers
            headers.Add("x-header-test-runner", "xunit");

            // Initialize header-tag arguments and expectations
            var headerToTagMap = new Dictionary<string, string>();
            var expectedResults = new Dictionary<string, string>();

            // Test
            var tagsFromHeader = headers.ExtractHeaderTags(headerToTagMap, TestPrefix);

            // Assert
            Assert.NotNull(tagsFromHeader);
            Assert.Equal(expectedResults, tagsFromHeader);
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void ExtractHeaderTags_ForEmptyStringMappings_CreatesNormalizedTagWithPrefix(IHeadersCollection headers)
        {
            string invalidCharacterSequence = "*|&#$%&^`.";
            string normalizedReplacementSequence = new string('_', invalidCharacterSequence.Length);

            // Add headers
            headers.Add("x-header-test-runner", "xunit");
            headers.Add($"x-header-1DATADOG-{invalidCharacterSequence}", "true");

            // Initialize header-tag arguments and expectations
            var headerToTagMap = new Dictionary<string, string>
            {
                { "x-header-test-runner", string.Empty },
                { $"x-header-1DATADOG-{invalidCharacterSequence}", string.Empty },
            };

            var expectedResults = new Dictionary<string, string>
            {
                { TestPrefix + "." + "x-header-test-runner", "xunit" },
                { TestPrefix + "." + $"x-header-1datadog-{normalizedReplacementSequence}", "true" }
            };

            // Test
            var tagsFromHeader = headers.ExtractHeaderTags(headerToTagMap, TestPrefix);

            // Assert
            Assert.NotNull(tagsFromHeader);
            Assert.Equal(expectedResults, tagsFromHeader);
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void Extract_EmptyHeadersReturnsNull(IHeadersCollection headers)
        {
            var resultContext = _propagator.Extract(headers);
            Assert.Null(resultContext);
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void InjectExtract_Identity(IHeadersCollection headers)
        {
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
        [MemberData(nameof(GetHeadersInvalidIdsCartesianProduct))]
        internal void Extract_InvalidTraceId(IHeadersCollection headers, string traceId)
        {
            const string spanId = "7";
            const string samplingPriority = "2";
            const string origin = "synthetics";

            InjectContext(headers, traceId, spanId, samplingPriority, origin);
            var resultContext = _propagator.Extract(headers);

            // invalid traceId should return a null context even if other values are set
            Assert.Null(resultContext);
        }

        [Theory]
        [MemberData(nameof(GetHeadersInvalidIdsCartesianProduct))]
        internal void Extract_InvalidSpanId(IHeadersCollection headers, string spanId)
        {
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
        [MemberData(nameof(GetHeadersInvalidSamplingPrioritiesCartesianProduct))]
        internal void Extract_InvalidSamplingPriority(IHeadersCollection headers, string samplingPriority)
        {
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
