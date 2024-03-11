// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
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

    public static IEnumerable<object[]> GetData()
    {
        foreach (var version in LibraryVersion.SqlClientSystem)
        {
#if NET6_0_OR_GREATER
            yield return new[] { version[0], true };
#endif
            yield return new[] { version[0], false };
        }
    }

    [SkippableTheory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(GetData))]
    public void SubmitTraces(string packageVersion, bool dbStatementForText)
    {
        // Skip the test if fixture does not support current platform
        _sqlServerFixture.SkipIfUnsupportedPlatform();

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_SQLCLIENT_SET_DBSTATEMENT_FOR_TEXT", dbStatementForText.ToString());
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        if (dbStatementForText)
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
}
