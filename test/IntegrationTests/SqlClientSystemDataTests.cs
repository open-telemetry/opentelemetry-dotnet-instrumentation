// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

public class SqlClientSystemDataTests : TestHelper
{
    public SqlClientSystemDataTests(ITestOutputHelper output)
        : base("SqlClient.System.NetFramework", output)
    {
    }

    [IgnoreRunningOnNet481Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitTraces()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("OpenTelemetry.Instrumentation.SqlClient");

        RunTestApplication();

        collector.AssertExpectations();
    }
}

public sealed class IgnoreRunningOnNet481Fact : FactAttribute
{
    public IgnoreRunningOnNet481Fact()
    {
        var netVersion = RuntimeHelper.GetRuntimeVersion();
        if (netVersion == "4.8.1+")
        {
            // https://github.com/open-telemetry/opentelemetry-dotnet/issues/3901
            Skip = "NET Framework 4.8.1 is skipped due bug.";
        }
    }
}
#endif
