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
        return LibraryVersion.SqlClientSystem;
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(GetData))]
    public void SubmitTraces(string packageVersion)
    {
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
}
