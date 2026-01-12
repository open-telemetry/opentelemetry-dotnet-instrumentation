// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

public class WcfNetFrameworkTests : WcfTestsBase
{
    public WcfNetFrameworkTests(ITestOutputHelper output)
        : base("Wcf.Client.NetFramework", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task SubmitsTraces()
    {
        await SubmitsTracesInternal(string.Empty).ConfigureAwait(true);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitsTracesNoEndpoint()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Status.Code == Status.Types.StatusCode.Error, "Client 1");

        collector.Expect("TestApplication.Wcf.Client.NetFramework", span => span.Kind == Span.Types.SpanKind.Internal, "Custom parent");
        collector.Expect("TestApplication.Wcf.Client.NetFramework", span => span.Kind == Span.Types.SpanKind.Internal, "Custom sibling");

        collector.ExpectCollected(WcfClientInstrumentation.ValidateExpectedSpanHierarchy);

        RunTestApplication(new TestSettings
        {
            PackageVersion = string.Empty
        });

        collector.AssertExpectations();
    }
}

#endif
