// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(PostgresCollectionFixture.Name)]
public class EntityFrameworkCoreNpgsqlTests : TestHelper
{
    private readonly PostgresFixture _postgres;

    public EntityFrameworkCoreNpgsqlTests(ITestOutputHelper output, PostgresFixture postgres)
        : base("EntityFrameworkCore.Npgsql", output)
    {
        _postgres = postgres;
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    public void SubmitTraces_DoesNotEmitEntityFrameworkCoreSpan()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        collector.Expect("Npgsql");
        collector.ExpectCollected(collected =>
            collected.All(span => span.Scope.Name != "OpenTelemetry.Instrumentation.EntityFrameworkCore"));

        RunTestApplication(new()
        {
            Arguments = $"--postgres {_postgres.Port}"
        });

        collector.AssertExpectations();
    }
}
#endif
