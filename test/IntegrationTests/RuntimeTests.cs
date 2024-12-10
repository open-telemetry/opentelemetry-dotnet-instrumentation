// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class RuntimeTests : TestHelper
{
    public RuntimeTests(ITestOutputHelper output)
        : base("Smoke", output)
    {
        SetEnvironmentVariable("LONG_RUNNING", "true");
        SetEnvironmentVariable("OTEL_METRIC_EXPORT_INTERVAL", "100");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitMetrics()
    {
        using var collector = new MockMetricsCollector(Output);
        SetExporter(collector);
#if NET9_0_OR_GREATER
        collector.Expect("System.Runtime");
#else
        collector.Expect("OpenTelemetry.Instrumentation.Runtime");
#endif

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
