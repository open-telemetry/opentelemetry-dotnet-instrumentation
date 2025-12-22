// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class HttpNetFrameworkTests : TestHelper
{
    public HttpNetFrameworkTests(ITestOutputHelper output)
        : base("Http.NetFramework", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task SubmitTraces()
    {
        using var collector = await MockSpansCollector.InitializeAsync(Output);
        SetExporter(collector);

        collector.Expect("OpenTelemetry.Instrumentation.Http.HttpWebRequest");

        RunTestApplication();

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task SubmitTracesFileBased()
    {
        using var collector = await MockSpansCollector.InitializeAsync(Output);
        SetFileBasedExporter(collector);
        EnableFileBasedConfigWithDefaultPath();

        collector.Expect("OpenTelemetry.Instrumentation.Http.HttpWebRequest");

        RunTestApplication();

        collector.AssertExpectations();
    }

    [Fact]
    public async Task SubmitTracesCapturesHttpHeaders()
    {
        using var collector = await MockSpansCollector.InitializeAsync(Output);
        SetExporter(collector);

        collector.Expect("OpenTelemetry.Instrumentation.Http.HttpWebRequest", span =>
        {
            return span.Attributes.Any(x => x.Key == "http.request.header.custom-request-test-header1" && x.Value.StringValue == "Test-Value1")
                   && span.Attributes.Any(x => x.Key == "http.request.header.custom-request-test-header3" && x.Value.StringValue == "Test-Value3")
                   && span.Attributes.All(x => x.Key != "http.request.header.custom-request-test-header2")
                   && span.Attributes.Any(x => x.Key == "http.response.header.custom-response-test-header2" && x.Value.StringValue == "Test-Value2")
                   && span.Attributes.All(x => x.Key != "http.response.header.custom-response-test-header1")
                   && span.Attributes.All(x => x.Key != "http.response.header.custom-response-test-header3");
        });

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_HTTP_INSTRUMENTATION_CAPTURE_REQUEST_HEADERS", "Custom-Request-Test-Header1,Custom-Request-Test-Header3");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_HTTP_INSTRUMENTATION_CAPTURE_RESPONSE_HEADERS", "Custom-Response-Test-Header2");

        RunTestApplication();

        collector.AssertExpectations();
    }
}
#endif
