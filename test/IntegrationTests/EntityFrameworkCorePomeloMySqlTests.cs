// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(MySqlCollection.Name)]
public class EntityFrameworkCorePomeloMySqlTests : TestHelper
{
    private readonly MySqlFixture _mySql;

    public EntityFrameworkCorePomeloMySqlTests(ITestOutputHelper output, MySqlFixture mySql)
        : base("EntityFrameworkCore.Pomelo.MySql", output)
    {
        _mySql = mySql;
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.EntityFrameworkCorePomeloMySql), MemberType = typeof(LibraryVersion))]
    public async Task SubmitsTraces(string packageVersion)
    {
        using var collector = await MockSpansCollector.InitializeAsync(Output);
        SetExporter(collector);
        collector.Expect("OpenTelemetry.Instrumentation.EntityFrameworkCore");

        RunTestApplication(new TestSettings
        {
            Arguments = $"--mysql {_mySql.Port}",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }
}
#endif
