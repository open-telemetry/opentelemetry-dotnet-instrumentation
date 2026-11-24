// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Trace.V1;

namespace IntegrationTests;

[Collection(SqlServerCollectionFixture.Name)]
public class AdoNetEntityFramework6Tests : TestHelper
{
    private const string AdoNetScopeName = "OpenTelemetry.AutoInstrumentation.AdoNet";
    private const string SqlClientScopeName = "OpenTelemetry.Instrumentation.SqlClient";

    // Sanitized form of the "SELECT 1" query run by the test application.
    private const string ApplicationQueryText = "SELECT ?";

    private readonly SqlServerFixture _sqlServerFixture;

    public AdoNetEntityFramework6Tests(ITestOutputHelper output, SqlServerFixture sqlServerFixture)
        : base("AdoNetEntityFramework6", output)
    {
        _sqlServerFixture = sqlServerFixture;
    }

    [SkippableFact]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    public void InterceptableDbCommandIsNotTraced()
    {
        // Skip the test if fixture does not support current platform
        _sqlServerFixture.SkipIfUnsupportedPlatform();

        // ADO.NET instrumentation is bytecode-based; without the profiler the test would pass trivially.
        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("TEST_DATABASE_PASSWORD", _sqlServerFixture.Password);

        // On .NET Framework, SqlClient records db.query.text only with IL rewrite enabled; it is needed to match the query.
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_SQLCLIENT_NETFX_ILREWRITE_ENABLED", bool.TrueString);

        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        // EF6 wraps the SqlCommand in InterceptableDbCommand. The query must be traced exactly once,
        // by the SqlClient instrumentation, with no duplicate span from the ADO.NET instrumentation.
        // EF6 also runs its own provider queries (e.g. serverproperty), so match on the query text.
        collector.Expect(SqlClientScopeName, IsProviderSpan, "Root microsoft.sql_server client span for the EF6 query.");
        collector.ExpectAllCollected(collected =>
        {
            Assert.DoesNotContain(collected, item => item.Scope.Name == AdoNetScopeName);
            Assert.Single(collected, item => IsApplicationQuery(item.Span));
            return true;
        });

        RunTestApplication(new()
        {
            Arguments = $"--port {_sqlServerFixture.Port}"
        });

        collector.AssertExpectations();
    }

    // Before the fix, this span was nested under the ADO.NET instrumentation's other_sql span.
    private static bool IsProviderSpan(Span span)
    {
        return IsApplicationQuery(span) &&
               span.Kind == Span.Types.SpanKind.Client &&
               span.ParentSpanId.IsEmpty &&
               span.Attributes.Any(attr => attr.Key == "db.system.name" && attr.Value.StringValue == "microsoft.sql_server");
    }

    private static bool IsApplicationQuery(Span span)
    {
        return span.Attributes.Any(attr => attr.Key == "db.query.text" && attr.Value.StringValue == ApplicationQueryText);
    }
}
