// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(OracleCollection.Name)]
public class OracleMdaTests : TestHelper
{
    private readonly OracleFixture _oracle;

    public OracleMdaTests(ITestOutputHelper output, OracleFixture oracle)
        : base("OracleMda", output)
    {
        _oracle = oracle;
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
#if NET6_0_OR_GREATER
    [MemberData(nameof(LibraryVersion.OracleMdaCore), MemberType = typeof(LibraryVersion))]
#else
    [MemberData(nameof(LibraryVersion.OracleMda), MemberType = typeof(LibraryVersion))]
#endif
    public void SubmitsTraces(string packageVersion)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
#if NET6_0_OR_GREATER
        collector.Expect("Oracle.ManagedDataAccess.Core");
#else
        collector.Expect("Oracle.ManagedDataAccess");
#endif

        RunTestApplication(new()
        {
#if NET462
            Framework = "net472",
#endif
            Arguments = $"--port {_oracle.Port} --password {_oracle.Password}",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }
}
