// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using FluentAssertions;
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class AssemblyRedirectionOnNetFrameworkTests : TestHelper
{
    public AssemblyRedirectionOnNetFrameworkTests(ITestOutputHelper output)
        : base("AssemblyRedirection.NetFramework", output)
    {
    }

    [Fact]
    public void SubmitsTraces()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        const string TestApplicationActivitySource = "AssemblyRedirection.NetFramework.ActivitySource";
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", TestApplicationActivitySource);
        collector.Expect(TestApplicationActivitySource);

        RunTestApplication();

        collector.AssertExpectations();
    }
}
#endif
