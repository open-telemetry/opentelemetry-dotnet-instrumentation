// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using IntegrationTests.Helpers;
using OpenTelemetry.AutoInstrumentation.Configurations;

namespace IntegrationTests;

public class SqlClientSystemDataTests : TestHelper
{
    public SqlClientSystemDataTests(ITestOutputHelper output)
        : base("SqlClient.System.NetFramework", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitTraces()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("OpenTelemetry.Instrumentation.SqlClient");

        RunTestApplication();

        collector.AssertExpectations();
    }

    [SkippableTheory]
    [Trait("Category", "EndToEnd")]
    [InlineData(true)]
    [InlineData(false)]
    public void SqlClientIlRewrite(bool enableIlRewrite)
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_SQLCLIENT_NETFX_ILREWRITE_ENABLED", enableIlRewrite.ToString());
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        if (enableIlRewrite)
        {
            collector.Expect("OpenTelemetry.Instrumentation.SqlClient", span => span.Attributes.Any(attr => attr.Key == "db.query.text" && !string.IsNullOrWhiteSpace(attr.Value?.StringValue)));
        }
        else
        {
            collector.Expect("OpenTelemetry.Instrumentation.SqlClient", span => span.Attributes.All(attr => attr.Key != "db.query.text"));
        }

        RunTestApplication();

        collector.AssertExpectations();
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [InlineData(false)]
    [InlineData(true)]
    public void PropagatesTraceContext(bool enableTransaction)
    {
        SetEnvironmentVariable(SqlClientTraceContextPropagationTestHelper.ContextPropagationEnvVar, bool.TrueString);

        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        var (standardOutput, _, _) = RunTestApplication(new()
        {
            Arguments = SqlClientTraceContextPropagationTestHelper.GetContextInfoCommandArguments(enableTransaction)
        });

        var contextInfo = SqlClientTraceContextPropagationTestHelper.ExtractContextInfo(standardOutput);

        collector.Expect(
            SqlClientTraceContextPropagationTestHelper.ScopeName,
            span => SqlClientTraceContextPropagationTestHelper.MatchesContextInfo(span, contextInfo),
            "SqlClient span matching propagated CONTEXT_INFO.");
        collector.ExpectAllCollected(
            collected => collected.Count(item => item.Scope.Name == SqlClientTraceContextPropagationTestHelper.ScopeName) == 1);
        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitMetrics()
    {
        using var collector = new MockMetricsCollector(Output);
        SetExporter(collector);

        collector.Expect("OpenTelemetry.Instrumentation.SqlClient");

        SetEnvironmentVariable("LONG_RUNNING", "true");
        SetEnvironmentVariable("OTEL_METRIC_EXPORT_INTERVAL", "100");
        SetEnvironmentVariable(ConfigurationKeys.Traces.TracesEnabled, bool.FalseString); // make sure that traces instrumentation is not needed

        using var process = StartTestApplication();

        try
        {
            collector.AssertExpectations();
        }
        finally
        {
            process?.Kill();
        }
    }
}
#endif
