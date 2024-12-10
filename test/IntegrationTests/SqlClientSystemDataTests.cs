// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using IntegrationTests.Helpers;
using OpenTelemetry.AutoInstrumentation.Configurations;
using Xunit.Abstractions;

namespace IntegrationTests;

public class SqlClientSystemDataTests : TestHelper
{
    public SqlClientSystemDataTests(ITestOutputHelper output)
        : base("SqlClient.System.NetFramework", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitTraces()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("OpenTelemetry.Instrumentation.SqlClient");

        RunTestApplication();

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitMetrics()
    {
        using var collector = new MockMetricsCollector(Output);
        SetExporter(collector);

        collector.Expect("OpenTelemetry.Instrumentation.SqlClient");

        SetEnvironmentVariable("LONG_RUNNING", "true");
        SetEnvironmentVariable("OTEL_METRIC_EXPORT_INTERVAL", "100");
        SetEnvironmentVariable(ConfigurationKeys.Traces.TracesEnabled, bool.FalseString); // make sure that traces instrumentation is not needed

        using var process = StartTestApplication();

        try
        {
            collector.AssertExpectations();
        }
        finally
        {
            process?.Kill();
        }
    }
}
#endif
