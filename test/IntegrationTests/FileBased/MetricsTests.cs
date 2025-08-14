// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests.FileBased;

public class MetricsTests : TestHelper
{
    public MetricsTests(ITestOutputHelper output)
     : base("Smoke", output)
    {
        SetEnvironmentVariable("LONG_RUNNING", "true");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitMetrics()
    {
        using var collector = new MockMetricsCollector(Output, 4318);
        SetExporter(collector);
        collector.Expect("OpenTelemetry.Instrumentation.Process");
        EnableFileBasedConfigWithDefaultPath();

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
