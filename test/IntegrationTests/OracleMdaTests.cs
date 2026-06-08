// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Google.Protobuf;
using IntegrationTests.Helpers;

namespace IntegrationTests;

[Collection(OracleCollectionFixture.Name)]
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

    public static TheoryData<string, bool> TestData()
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
    [MemberData(nameof(TestData))]
    public async Task SubmitTraces(string packageVersion, bool dbStatementForText)
    {
        // Skip the test if fixture does not support current platform
        _oracle.SkipIfUnsupportedPlatform();

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_ORACLEMDA_SET_DBSTATEMENT_FOR_TEXT", dbStatementForText.ToString());
        SetEnvironmentVariable("TNS_ADMIN", _oracle.WalletDirectory);
        EnableBytecodeInstrumentation();

        using var collector = new MockSpansCollector(Output);
        var databaseOpenTelemetryTracingSupported = IsDatabaseOpenTelemetryTracingSupported(packageVersion);
        SetExporter(collector);

        if (databaseOpenTelemetryTracingSupported)
        {
            await _oracle.EnableOpenTelemetryTracingAsync().ConfigureAwait(false);
        }

#if  NETFRAMEWORK
        const string instrumentationScopeName = "Oracle.ManagedDataAccess";
#else
        const string instrumentationScopeName = "Oracle.ManagedDataAccess.Core";
#endif

        ByteString? databaseParentSpanId = null;
        ByteString? databaseTraceId = null;
        collector.Expect(
            instrumentationScopeName,
            span =>
            {
                var dbStatementMatches = dbStatementForText
                    ? span.Attributes.Any(attr => attr.Key == "db.statement" && !string.IsNullOrWhiteSpace(attr.Value?.StringValue))
                    : span.Attributes.All(attr => attr.Key != "db.statement");
                if (!dbStatementMatches)
                {
                    return false;
                }

                if (databaseOpenTelemetryTracingSupported)
                {
                    if (span.Name != "SendExecuteRequest" ||
                        span.Kind != OpenTelemetry.Proto.Trace.V1.Span.Types.SpanKind.Client)
                    {
                        return false;
                    }

                    databaseParentSpanId = span.SpanId;
                    databaseTraceId = span.TraceId;
                }

                return true;
            });

        RunTestApplication(new()
        {
#if NET462
            Framework = "net472",
#endif
            Arguments = $"--user {_oracle.User} --password {_oracle.Password} --data-source {_oracle.DataSource}",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();

        if (databaseOpenTelemetryTracingSupported)
        {
            if (databaseTraceId == null || databaseParentSpanId == null)
            {
                throw new InvalidOperationException("ODP.NET database round-trip span was not collected.");
            }

            var databaseTraceIdHex = ToLowerHex(databaseTraceId);
            var databaseParentSpanIdHex = ToLowerHex(databaseParentSpanId);
            await _oracle.AssertTraceContextArchivedAsync(databaseTraceIdHex, databaseParentSpanIdHex).ConfigureAwait(false);
            Output.WriteLine($"Oracle database archived propagated context: traceid-{databaseTraceIdHex}:parentid-{databaseParentSpanIdHex}");
        }
    }

    private static bool IsDatabaseOpenTelemetryTracingSupported(string packageVersion)
    {
        return string.IsNullOrEmpty(packageVersion) || (Version.TryParse(packageVersion, out var version) && version.CompareTo(new Version(23, 26, 200)) >= 0);
    }

    private static string ToLowerHex(ByteString bytes)
    {
        return string.Concat(bytes.ToByteArray().Select(value => value.ToString("x2", System.Globalization.CultureInfo.InvariantCulture)));
    }
}
