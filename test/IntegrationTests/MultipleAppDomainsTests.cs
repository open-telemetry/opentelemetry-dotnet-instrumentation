// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class MultipleAppDomainsTests : TestHelper
{
    public MultipleAppDomainsTests(ITestOutputHelper output)
        : base("MultipleAppDomains.NetFramework", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitsTraces()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        const int expectedSpanCount = 5;
        for (var i = 0; i < expectedSpanCount; i++)
        {
            collector.Expect("ByteCode.Plugin.StrongNamedValidation");
        }

        // Use the integrations file that bring the expected instrumentation.
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "ByteCode.Plugin.StrongNamedValidation");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestLibrary.InstrumentationTarget.Plugin, TestLibrary.InstrumentationTarget, Version=1.0.0.0, Culture=neutral, PublicKeyToken=c0db600a13f60b51");
        var (_, standardErrorOutput, _) = RunTestApplication();

        // Nothing regarding log should have been logged to the console.
        Assert.DoesNotContain("Log:", standardErrorOutput, StringComparison.Ordinal);

        collector.AssertExpectations();
    }
}
#endif
