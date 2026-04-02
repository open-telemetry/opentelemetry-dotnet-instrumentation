// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(MySqlCollectionFixture.Name)]
public class MySqlDataTests : TestHelper
{
    private readonly MySqlFixture _mySql;

    public MySqlDataTests(ITestOutputHelper output, MySqlFixture mySql)
        : base("MySqlData", output)
    {
        _mySql = mySql;
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.MySqlData), MemberType = typeof(LibraryVersion))]
    public void SubmitsTraces(string packageVersion)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("connector-net");

        EnableBytecodeInstrumentation();
        RunTestApplication(new()
        {
            Arguments = $"--mysql {_mySql.Port}",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }
}
#endif
