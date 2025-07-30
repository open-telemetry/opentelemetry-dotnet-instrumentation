// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests.FileBased;

public class TrasesTests : TestHelper
{
    public TrasesTests(ITestOutputHelper output)
        : base("Http", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitTraces()
    {
        using var collector = new MockSpansCollector(Output, 4318);
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

        SetEnvironmentVariable("OTEL_EXPERIMENTAL_FILE_BASED_CONFIGURATION_ENABLED", "true");

        RunTestApplication();

        collector.AssertExpectations();

        // testing context propagation via trace hierarchy
        Assert.True(clientSpan!.ParentSpanId.IsEmpty, "parent of client span should be empty");
        Assert.Equal(clientSpan.SpanId, serverSpan!.ParentSpanId);
        Assert.Equal(serverSpan.SpanId, manualSpan!.ParentSpanId);
    }
}
#endif

