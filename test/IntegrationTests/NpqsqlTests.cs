// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(PostgresCollectionFixture.Name)]
public class NpqsqlTests : TestHelper
{
    private readonly PostgresFixture _postgres;

    public NpqsqlTests(ITestOutputHelper output, PostgresFixture postgres)
        : base("Npgsql", output)
    {
        _postgres = postgres;
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.Npgsql), MemberType = typeof(LibraryVersion))]
    public void SubmitTraces(string packageVersion)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("Npgsql");

        RunTestApplication(new()
        {
            Arguments = $"--postgres {_postgres.Port}",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }

#if NET
    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.Npgsql), MemberType = typeof(LibraryVersion))]
    public void SubmitMetrics(string packageVersion)
    {
        using var collector = new MockMetricsCollector(Output);
        SetExporter(collector);
        collector.Expect("Npgsql");

        RunTestApplication(new()
        {
            Arguments = $"--postgres {_postgres.Port}",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }
#endif
}
