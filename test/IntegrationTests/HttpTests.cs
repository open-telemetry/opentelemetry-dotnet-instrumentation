// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER
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
#if NET7_0_OR_GREATER
        collector.Expect("System.Net.Http", span =>
#else
        collector.Expect("OpenTelemetry.Instrumentation.Http.HttpClient", span =>
#endif
        {
            clientSpan = span;
            return true;
        });
        Span? serverSpan = null;
#if NET7_0_OR_GREATER
        collector.Expect("Microsoft.AspNetCore", span =>
#else
        collector.Expect("OpenTelemetry.Instrumentation.AspNetCore", span =>
#endif
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
    [Trait("Category", "EndToEnd")]
    public void SubmitMetrics()
    {
        using var collector = new MockMetricsCollector(Output);
        SetExporter(collector);
#if NET8_0_OR_GREATER
        collector.Expect("System.Net.Http");
        collector.Expect("System.Net.NameResolution");
        collector.Expect("Microsoft.AspNetCore.Hosting");
        collector.Expect("Microsoft.AspNetCore.Server.Kestrel");
        collector.Expect("Microsoft.AspNetCore.Http.Connections");
        collector.Expect("Microsoft.AspNetCore.Routing");
        collector.Expect("Microsoft.AspNetCore.Diagnostics");
        collector.Expect("Microsoft.AspNetCore.RateLimiting");
        collector.ExpectAdditionalEntries(x => x.All(m => m.InstrumentationScopeName != "OpenTelemetry.Instrumentation.AspNetCore" && m.InstrumentationScopeName != "OpenTelemetry.Instrumentation.Http"));
#else
        collector.Expect("OpenTelemetry.Instrumentation.AspNetCore");
        collector.Expect("OpenTelemetry.Instrumentation.Http");
#endif

        RunTestApplication();

        collector.AssertExpectations();
    }
}
#endif
