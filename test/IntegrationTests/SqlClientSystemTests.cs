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

    public static TheoryData<string, bool> GetData()
    {
        var theoryData = new TheoryData<string, bool>();

        foreach (var version in LibraryVersion.SqlClientSystem)
        {
            theoryData.Add(version, true);
            theoryData.Add(version, false);
        }

        return theoryData;
    }

    [SkippableTheory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(GetData))]
    public void SubmitTraces(string packageVersion, bool enableIlRewrite)
    {
        // Skip the test if fixture does not support current platform
        _sqlServerFixture.SkipIfUnsupportedPlatform();

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_SQLCLIENT_SET_DBSTATEMENT_FOR_TEXT", "true");
#if NETFRAMEWORK
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_SQLCLIENT_NETFX_ILREWRITE_ENABLED", enableIlRewrite.ToString());
#else
        enableIlRewrite.ToString(); // to avoid unused parameter warning
#endif
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

#if NETFRAMEWORK
        if (enableIlRewrite)
        {
            collector.Expect("OpenTelemetry.Instrumentation.SqlClient", span => span.Attributes.Any(attr => attr.Key == "db.statement" && !string.IsNullOrWhiteSpace(attr.Value?.StringValue)));
        }
        else
        {
            collector.Expect("OpenTelemetry.Instrumentation.SqlClient", span => span.Attributes.All(attr => attr.Key != "db.statement"));
        }
#else
        collector.Expect("OpenTelemetry.Instrumentation.SqlClient", span => span.Attributes.Any(attr => attr.Key == "db.statement" && !string.IsNullOrWhiteSpace(attr.Value?.StringValue)));
#endif

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
