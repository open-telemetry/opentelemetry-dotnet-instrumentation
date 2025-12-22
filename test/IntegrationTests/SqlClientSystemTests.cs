// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using OpenTelemetry.AutoInstrumentation.Configurations;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(SqlServerCollection.Name)]
public class SqlClientSystemTests : TestHelper
{
    private readonly SqlServerFixture _sqlServerFixture;

    public SqlClientSystemTests(ITestOutputHelper output, SqlServerFixture sqlServerFixture)
        : base("SqlClient.System", output)
    {
        _sqlServerFixture = sqlServerFixture;
    }

    public static TheoryData<string, bool, bool> GetDataForIlRewrite()
    {
        var theoryData = new TheoryData<string, bool, bool>();

        foreach (var version in LibraryVersion.SqlClientSystem)
        {
            theoryData.Add(version, true, true);
            theoryData.Add(version, true, false);
            theoryData.Add(version, false, true);
            theoryData.Add(version, false, false);
        }

        return theoryData;
    }

    [SkippableTheory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.SqlClientSystem), MemberType = typeof(LibraryVersion))]
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

#if NETFRAMEWORK
    [SkippableTheory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(GetDataForIlRewrite))]
    public void SqlClientIlRewrite(string packageVersion, bool enableIlRewrite, bool isFileBased)
    {
        // Skip the test if fixture does not support current platform
        _sqlServerFixture.SkipIfUnsupportedPlatform();

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_SQLCLIENT_NETFX_ILREWRITE_ENABLED", enableIlRewrite.ToString());
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

        if (enableIlRewrite)
        {
            collector.Expect("OpenTelemetry.Instrumentation.SqlClient", span => span.Attributes.Any(attr => attr.Key == "db.statement" && !string.IsNullOrWhiteSpace(attr.Value?.StringValue)));
        }
        else
        {
            collector.Expect("OpenTelemetry.Instrumentation.SqlClient", span => span.Attributes.All(attr => attr.Key != "db.statement"));
        }

        RunTestApplication(new()
        {
            Arguments = $"{_sqlServerFixture.Password} {_sqlServerFixture.Port}",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }
#endif

    [SkippableTheory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.SqlClientSystem), MemberType = typeof(LibraryVersion))]
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
