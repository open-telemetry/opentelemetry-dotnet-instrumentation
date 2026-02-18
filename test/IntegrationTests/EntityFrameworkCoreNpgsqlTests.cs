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
    public void SubmitTraces_NpgsqlSpanHasApplicationSpanAsParent()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        collector.Expect("TestApplication.EntityFrameworkCore.Npgsql");
        collector.Expect("Npgsql");
        collector.ExpectCollected(collected =>
        {
            var applicationSpan = collected.First(span => span.Scope.Name == "TestApplication.EntityFrameworkCore.Npgsql");
            var npgsqlSpan = collected.First(span => span.Scope.Name == "Npgsql");

            return npgsqlSpan.Span.ParentSpanId.Equals(applicationSpan.Span.SpanId);
        });

        RunTestApplication(new()
        {
            Arguments = $"--postgres {_postgres.Port}"
        });

        collector.AssertExpectations();
    }
}
#endif
