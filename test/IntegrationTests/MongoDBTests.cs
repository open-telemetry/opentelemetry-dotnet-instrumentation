// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(MongoDBCollection.Name)]
public class MongoDBTests : TestHelper
{
    private readonly MongoDBFixture _mongoDB;

    public MongoDBTests(ITestOutputHelper output, MongoDBFixture mongoDB)
        : base("MongoDB", output)
    {
        _mongoDB = mongoDB;
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Any")]
    [MemberData(nameof(LibraryVersion.MongoDB), MemberType = typeof(LibraryVersion))]
    public void SubmitsTraces(string packageVersion)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        const int spanCount = 3;
        for (int i = 0; i < spanCount; i++)
        {
            collector.Expect("MongoDB.Driver.Core.Extensions.DiagnosticSources");
        }

        EnableBytecodeInstrumentation();
        RunTestApplication(new()
        {
            Arguments = $"--mongo-db {_mongoDB.Port}",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }
}
