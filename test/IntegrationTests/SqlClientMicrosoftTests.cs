// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using OpenTelemetry.AutoInstrumentation.Configurations;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(SqlServerCollectionFixture.Name)]
public class SqlClientMicrosoftTests : TestHelper
{
    private readonly SqlServerFixture _sqlServerFixture;

    public SqlClientMicrosoftTests(ITestOutputHelper output, SqlServerFixture sqlServerFixture)
        : base("SqlClient.Microsoft", output)
    {
        _sqlServerFixture = sqlServerFixture;
    }

    [SkippableTheory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.SqlClientMicrosoft), MemberType = typeof(LibraryVersion))]
    public void SubmitTraces(string packageVersion)
    {
        // Skip the test if fixture does not support current platform
        _sqlServerFixture.SkipIfUnsupportedPlatform();

        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("OpenTelemetry.Instrumentation.SqlClient");

        RunTestApplication(new()
        {
            Arguments = $"{_sqlServerFixture.Password} {_sqlServerFixture.Port}",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }

    [SkippableTheory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.SqlClientMicrosoft), MemberType = typeof(LibraryVersion))]
    public void SubmitMetrics(string packageVersion)
    {
        // Skip the test if fixture does not support current platform
        _sqlServerFixture.SkipIfUnsupportedPlatform();

        using var collector = new MockMetricsCollector(Output);
        SetExporter(collector);

        collector.Expect("OpenTelemetry.Instrumentation.SqlClient");

        SetEnvironmentVariable("LONG_RUNNING", "true");
        SetEnvironmentVariable("OTEL_METRIC_EXPORT_INTERVAL", "100");
        SetEnvironmentVariable(ConfigurationKeys.Traces.TracesEnabled, bool.FalseString); // make sure that traces instrumentation is not needed

        using var process = StartTestApplication(new TestSettings
        {
            Arguments = $"{_sqlServerFixture.Password} {_sqlServerFixture.Port}",
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
