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

    public static IEnumerable<object[]> GetData()
    {
#if NETFRAMEWORK
        foreach (var version in LibraryVersion.OracleMda)
#else
        foreach (var version in LibraryVersion.OracleMdaCore)
#endif
        {
            yield return new[] { version[0], true };
            yield return new[] { version[0], false };
        }
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(GetData))]
    public void SubmitTraces(string packageVersion, bool dbStatementForText)
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_ORACLEMDA_SET_DBSTATEMENT_FOR_TEXT", dbStatementForText.ToString());

        using var collector = new MockSpansCollector(Output);
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
