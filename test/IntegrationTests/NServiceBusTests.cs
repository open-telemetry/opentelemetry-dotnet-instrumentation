// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using OpenTelemetry.AutoInstrumentation.Configurations;
using Xunit.Abstractions;

namespace IntegrationTests;

public class NServiceBusTests : TestHelper
{
    public NServiceBusTests(ITestOutputHelper output)
        : base("NServiceBus", output)
    {
        EnableBytecodeInstrumentation();
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.NServiceBus), MemberType = typeof(LibraryVersion))]
    public async Task SubmitsTraces(string packageVersion)
    {
        using var collector = await MockSpansCollector.InitializeAsync(Output);
        SetExporter(collector);
        collector.Expect("NServiceBus.Core");
        SetEnvironmentVariable(ConfigurationKeys.Metrics.MetricsEnabled, bool.FalseString); // make sure that metrics instrumentation is not needed

        using var process = StartTestApplication(new TestSettings
        {
#if NET462
            Framework = "net472",
#endif
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.NServiceBus), MemberType = typeof(LibraryVersion))]
    public async Task SubmitMetrics(string packageVersion)
    {
        using var collector = await MockMetricsCollector.InitializeAsync(Output);
        SetExporter(collector);

#if NET
        if (string.IsNullOrEmpty(packageVersion) || Version.Parse(packageVersion) >= new Version(9, 1))
        {
            collector.Expect("NServiceBus.Core.Pipeline.Incoming");
        }
        else
        {
            collector.Expect("NServiceBus.Core");
        }
#else
        collector.Expect("NServiceBus.Core");
#endif

        SetEnvironmentVariable("LONG_RUNNING", "true");
        SetEnvironmentVariable("OTEL_METRIC_EXPORT_INTERVAL", "100");
        SetEnvironmentVariable(ConfigurationKeys.Traces.TracesEnabled, bool.FalseString); // make sure that traces instrumentation is not needed

        using var process = StartTestApplication(new TestSettings
        {
#if NET462
            Framework = "net472",
#endif
            PackageVersion = packageVersion
        });

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
