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
#if NETFRAMEWORK
        : base("OracleMda.NetFramework", output)
#else
        : base("OracleMda.Core", output)
#endif
    {
        _oracle = oracle;
    }

    public static TheoryData<string, bool> GetData()
    {
        var theoryData = new TheoryData<string, bool>();

#if NETFRAMEWORK
        foreach (var version in LibraryVersion.OracleMda)
#else
        foreach (var version in LibraryVersion.GetPlatformVersions(nameof(LibraryVersion.OracleMdaCore)))
#endif
        {
            theoryData.Add(version, true);
            theoryData.Add(version, false);
        }

        return theoryData;
    }

    [SkippableTheory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(GetData))]
    public async Task SubmitTraces(string packageVersion, bool dbStatementForText)
    {
        // Skip the test if fixture does not support current platform
        _oracle.SkipIfUnsupportedPlatform();

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_ORACLEMDA_SET_DBSTATEMENT_FOR_TEXT", dbStatementForText.ToString());

        using var collector = await MockSpansCollector.InitializeAsync(Output);
        SetExporter(collector);

#if  NETFRAMEWORK
        const string instrumentationScopeName = "Oracle.ManagedDataAccess";
#else
        const string instrumentationScopeName = "Oracle.ManagedDataAccess.Core";
#endif

        if (dbStatementForText)
        {
            collector.Expect(instrumentationScopeName, span => span.Attributes.Any(attr => attr.Key == "db.statement" && !string.IsNullOrWhiteSpace(attr.Value?.StringValue)));
        }
        else
        {
            collector.Expect(instrumentationScopeName, span => span.Attributes.All(attr => attr.Key != "db.statement"));
        }

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
