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

    public static TheoryData<string, bool, bool> TestData()
    {
        var theoryData = new TheoryData<string, bool, bool>();

#if NETFRAMEWORK
        foreach (var version in LibraryVersion.OracleMda)
#else
        foreach (var version in LibraryVersion.GetPlatformVersions(nameof(LibraryVersion.OracleMdaCore)))
#endif
        {
            var databaseOpenTelemetryTracingSupported = IsDatabaseOpenTelemetryTracingSupported(version);
            theoryData.Add(version, true, databaseOpenTelemetryTracingSupported);
            theoryData.Add(version, false, databaseOpenTelemetryTracingSupported);
        }

        return theoryData;
    }

    [SkippableTheory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(TestData))]
    public async Task SubmitTraces(string packageVersion, bool dbStatementForText, bool databaseOpenTelemetryTracingSupported)
    {
        // Skip the test if fixture does not support current platform
        _oracle.SkipIfUnsupportedPlatform();

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_ORACLEMDA_SET_DBSTATEMENT_FOR_TEXT", dbStatementForText.ToString());
        SetEnvironmentVariable("TNS_ADMIN", _oracle.WalletDirectory);
        EnableBytecodeInstrumentation();

        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

#if NET
        using var databaseCollector = databaseOpenTelemetryTracingSupported
            ? new MockSpansCollector(Output, _oracle.DatabaseOpenTelemetryCertificate.ServerCertificate)
            : null;
#endif

        if (databaseOpenTelemetryTracingSupported)
        {
#if NET
            databaseCollector!.ResourceExpector.Expect("service.name", "oracle-db");
            databaseCollector.Expect(
                string.Empty,
                span =>
                    string.Equals(span.Name, "DB Server", StringComparison.Ordinal) &&
                    span.Kind == OpenTelemetry.Proto.Trace.V1.Span.Types.SpanKind.Server,
                "Oracle DB server-side span");
            Output.WriteLine(await _oracle.AssertOpenTelemetryTraceEndpointReachableAsync(databaseCollector.Port).ConfigureAwait(false));
            await _oracle.EnableOpenTelemetryTracingAsync(databaseCollector.Port).ConfigureAwait(false);
            Output.WriteLine(await _oracle.GetOpenTelemetryServiceStatusAsync().ConfigureAwait(false));
#else
            await _oracle.EnableOpenTelemetryTracingAsync().ConfigureAwait(false);
#endif
        }

#if  NETFRAMEWORK
        const string instrumentationScopeName = "Oracle.ManagedDataAccess";
#else
        const string instrumentationScopeName = "Oracle.ManagedDataAccess.Core";
#endif

        string? databaseTraceIdHex = null;
        string? databaseParentSpanIdHex = null;
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
                    if (!IsDatabaseRoundTripSpan(span))
                    {
                        return false;
                    }

                    databaseTraceIdHex = ToLowerHex(span.TraceId);
                    databaseParentSpanIdHex = ToLowerHex(span.SpanId);
                }

                return true;
            });

        var testSettings = new TestSettings
        {
#if NET462
            Framework = "net472",
#endif
            Arguments = $"--user {_oracle.User} --password {_oracle.Password} --data-source {_oracle.DataSource}",
            PackageVersion = packageVersion
        };

        try
        {
            RunTestApplication(testSettings);
        }
        catch
        {
            if (databaseOpenTelemetryTracingSupported)
            {
                Output.WriteLine(await _oracle.GetOpenTelemetryServiceStatusAsync().ConfigureAwait(false));
                Output.WriteLine(await _oracle.GetOpenTelemetryTraceDiagnosticsAsync().ConfigureAwait(false));
            }

            throw;
        }

        collector.AssertExpectations();

        if (databaseOpenTelemetryTracingSupported)
        {
            if (databaseTraceIdHex == null || databaseParentSpanIdHex == null)
            {
                throw new InvalidOperationException("ODP.NET database round-trip span was not collected.");
            }

            await _oracle.AssertTraceContextArchivedAsync(databaseTraceIdHex, databaseParentSpanIdHex).ConfigureAwait(false);
            Output.WriteLine($"Oracle database archived propagated context: traceid-{databaseTraceIdHex}:parentid-{databaseParentSpanIdHex}");
            Output.WriteLine(await _oracle.GetOpenTelemetryServiceStatusAsync().ConfigureAwait(false));
            Output.WriteLine(await _oracle.GetOpenTelemetryTraceDiagnosticsAsync().ConfigureAwait(false));

#if NET
            databaseCollector!.ResourceExpector.AssertExpectations();
            databaseCollector.AssertExpectations();
#endif
        }
    }

    private static bool IsDatabaseOpenTelemetryTracingSupported(string packageVersion)
    {
        return string.IsNullOrEmpty(packageVersion) || (Version.TryParse(packageVersion, out var version) && version.CompareTo(new Version(23, 26, 200)) >= 0);
    }

    private static bool IsDatabaseRoundTripSpan(OpenTelemetry.Proto.Trace.V1.Span span)
    {
        return span.Name == "SendExecuteRequest" &&
               span.Kind == OpenTelemetry.Proto.Trace.V1.Span.Types.SpanKind.Client;
    }

    private static string ToLowerHex(ByteString bytes)
    {
        return string.Concat(bytes.ToByteArray().Select(value => value.ToString("x2", System.Globalization.CultureInfo.InvariantCulture)));
    }
}
