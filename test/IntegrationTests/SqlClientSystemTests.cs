// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Trace.V1;
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
            yield return new[] { version[0], true };
            yield return new[] { version[0], false };
        }
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(GetData))]
    public void SubmitTraces(string packageVersion, bool dbStatementForText)
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_SQLCLIENT_SET_DBSTATEMENT_FOR_TEXT", dbStatementForText.ToString());
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("OpenTelemetry.Instrumentation.SqlClient", span => Expect(span, dbStatementForText));

        RunTestApplication(new()
        {
            Arguments = $"{_sqlServerFixture.Password} {_sqlServerFixture.Port}",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }

    private static bool Expect(Span span, bool dbStatementForText)
    {
        if (dbStatementForText && !span.Attributes.Any(attr => attr.Key == "db.statement" && !string.IsNullOrWhiteSpace(attr.Value?.StringValue)))
        {
            return false;
        }

        if (!dbStatementForText && span.Attributes.Any(attr => attr.Key == "db.statement"))
        {
            return false;
        }

        return true;
    }
}
