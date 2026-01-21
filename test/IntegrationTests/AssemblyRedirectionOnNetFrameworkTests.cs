// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class AssemblyRedirectionOnNetFrameworkTests : TestHelper
{
    public AssemblyRedirectionOnNetFrameworkTests(ITestOutputHelper output)
        : base("AssemblyRedirection.NetFramework", output)
    {
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void SubmitsTraces(bool isFileBased, bool isNetFxRedirectEnabled)
    {
        using var collector = new MockSpansCollector(Output);
        if (isFileBased)
        {
            SetFileBasedExporter(collector);
            EnableFileBasedConfigWithDefaultPath();
        }
        else
        {
            SetExporter(collector);
        }

        const string TestApplicationActivitySource = "AssemblyRedirection.NetFramework.ActivitySource";
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", TestApplicationActivitySource);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_NETFX_REDIRECT_ENABLED", isNetFxRedirectEnabled.ToString());
        collector.Expect(TestApplicationActivitySource);

        RunTestApplication();

        collector.AssertExpectations();
    }
}
#endif
