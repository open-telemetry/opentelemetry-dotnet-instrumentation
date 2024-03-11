// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(SqlServerCollection.Name)]
public class SqlClientMicrosoftTests : TestHelper
{
    private readonly SqlServerFixture _sqlServerFixture;

    public SqlClientMicrosoftTests(ITestOutputHelper output, SqlServerFixture sqlServerFixture)
        : base("SqlClient.Microsoft", output)
    {
        _sqlServerFixture = sqlServerFixture;
    }

    public static IEnumerable<object[]> GetData()
    {
#if NETFRAMEWORK
        // 3.1.* is not supported on .NET Framework. For details check: https://github.com/open-telemetry/opentelemetry-dotnet/issues/4243
        return LibraryVersion.SqlClientMicrosoft.Where(x => !x.First().ToString().StartsWith("3.1."));
#else
        return LibraryVersion.SqlClientMicrosoft;
#endif
    }

    [SkippableTheory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(GetData))]
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
}
