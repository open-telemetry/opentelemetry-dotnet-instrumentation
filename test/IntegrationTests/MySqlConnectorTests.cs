// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(MySqlCollection.Name)]
public class MySqlConnectorTests : TestHelper
{
    private readonly MySqlFixture _mySql;

    public MySqlConnectorTests(ITestOutputHelper output, MySqlFixture mySql)
        : base("MySqlConnector", output)
    {
        _mySql = mySql;
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.MySqlConnector), MemberType = typeof(LibraryVersion))]
    public async Task SubmitsTraces(string packageVersion)
    {
        using var collector = await MockSpansCollector.InitializeAsync(Output);
        SetExporter(collector);
        collector.Expect("MySqlConnector");

        RunTestApplication(new()
        {
            Arguments = $"--mysql {_mySql.Port}",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }
}
