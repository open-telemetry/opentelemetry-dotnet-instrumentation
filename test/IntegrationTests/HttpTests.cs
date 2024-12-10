// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using FluentAssertions;
using FluentAssertions.Execution;
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

public class HttpTests : TestHelper
{
    public HttpTests(ITestOutputHelper output)
        : base("Http", output)
    {
    }

    [Theory]
    [InlineData("")] // equivalent of default value
    [InlineData("b3multi")]
    [InlineData("b3")]
    [Trait("Category", "EndToEnd")]
    public void SubmitTraces(string propagators)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        Span? clientSpan = null;
        collector.Expect("System.Net.Http", span =>
        {
            clientSpan = span;
            return true;
        });

        Span? serverSpan = null;
        collector.Expect("Microsoft.AspNetCore", span =>
        {
            serverSpan = span;
            return true;
        });

        Span? manualSpan = null;
        collector.Expect("TestApplication.Http", span =>
        {
            manualSpan = span;
            return true;
        });

        SetEnvironmentVariable("OTEL_PROPAGATORS", propagators);
        SetEnvironmentVariable("DISABLE_DistributedContextPropagator", "true");
        RunTestApplication();

        collector.AssertExpectations();
        using (new AssertionScope())
        {
            // testing context propagation via trace hierarchy
            clientSpan!.ParentSpanId.IsEmpty.Should().BeTrue();
            serverSpan!.ParentSpanId.Should().Equal(clientSpan.SpanId);
            manualSpan!.ParentSpanId.Should().Equal(serverSpan.SpanId);
        }
    }

    [Fact]
    public void SubmitTracesCapturesHttpHeaders()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        collector.Expect("System.Net.Http", span =>
        {
            return span.Attributes.Any(x => x.Key == "http.request.header.custom-request-test-header1" && x.Value.StringValue == "Test-Value1")
                   && span.Attributes.Any(x => x.Key == "http.request.header.custom-request-test-header3" && x.Value.StringValue == "Test-Value3")
                   && span.Attributes.All(x => x.Key != "http.request.header.custom-request-test-header2")
                   && span.Attributes.Any(x => x.Key == "http.response.header.custom-response-test-header2" && x.Value.StringValue == "Test-Value2")
                   && span.Attributes.All(x => x.Key != "http.response.header.custom-response-test-header1")
                   && span.Attributes.All(x => x.Key != "http.response.header.custom-response-test-header3");
        });

        collector.Expect("Microsoft.AspNetCore", span =>
        {
            return span.Attributes.Any(x => x.Key == "http.request.header.custom-request-test-header2" && x.Value.StringValue == "Test-Value2")
                   && span.Attributes.All(x => x.Key != "http.request.header.custom-request-test-header1")
                   && span.Attributes.All(x => x.Key != "http.request.header.custom-request-test-header3")
                   && span.Attributes.Any(x => x.Key == "http.response.header.custom-response-test-header1" && x.Value.StringValue == "Test-Value1")
                   && span.Attributes.Any(x => x.Key == "http.response.header.custom-response-test-header3" && x.Value.StringValue == "Test-Value3")
                   && span.Attributes.All(x => x.Key != "http.response.header.custom-response-test-header2");
        });

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_HTTP_INSTRUMENTATION_CAPTURE_REQUEST_HEADERS", "Custom-Request-Test-Header1,Custom-Request-Test-Header3");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_HTTP_INSTRUMENTATION_CAPTURE_RESPONSE_HEADERS", "Custom-Response-Test-Header2");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ASPNETCORE_INSTRUMENTATION_CAPTURE_REQUEST_HEADERS", "Custom-Request-Test-Header2");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ASPNETCORE_INSTRUMENTATION_CAPTURE_RESPONSE_HEADERS", "Custom-Response-Test-Header1,Custom-Response-Test-Header3");

        RunTestApplication();

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitMetrics()
    {
        using var collector = new MockMetricsCollector(Output);
        SetExporter(collector);
        collector.Expect("System.Net.Http");
        collector.Expect("System.Net.NameResolution");
        collector.Expect("Microsoft.AspNetCore.Hosting");
        collector.Expect("Microsoft.AspNetCore.Server.Kestrel");
        collector.Expect("Microsoft.AspNetCore.Http.Connections");
        collector.Expect("Microsoft.AspNetCore.Routing");
        collector.Expect("Microsoft.AspNetCore.Diagnostics");
        collector.Expect("Microsoft.AspNetCore.RateLimiting");
        collector.ExpectAdditionalEntries(x => x.All(m => m.InstrumentationScopeName != "OpenTelemetry.Instrumentation.Http"));

        RunTestApplication();

        collector.AssertExpectations();
    }
}
#endif
