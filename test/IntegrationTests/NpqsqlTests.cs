// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Trace.V1;

namespace IntegrationTests;

[Collection(PostgresCollectionFixture.Name)]
public class NpqsqlTests : TestHelper
{
    private const string ContextPropagationApplicationName = "otel-context-probe";
#if NET
    private const string StaleContextApplicationName = "otel-stale-probe";
#endif
    private const string MultiplexingApplicationName = "otel-multiplexing-probe";

    private readonly PostgresFixture _postgres;

    public NpqsqlTests(ITestOutputHelper output, PostgresFixture postgres)
        : base("Npgsql", output)
    {
        _postgres = postgres;
    }

#if NET
    public static TheoryData<string> NpgsqlWithTracingFilters()
    {
        var theoryData = new TheoryData<string>();
        foreach (var version in LibraryVersion.Npgsql)
        {
            if (string.IsNullOrEmpty(version) || Version.Parse(version).Major >= 9)
            {
                theoryData.Add(version);
            }
        }

        return theoryData;
    }
#endif

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.Npgsql), MemberType = typeof(LibraryVersion))]
    public void SubmitTraces(string packageVersion)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("Npgsql");

        RunTestApplication(new()
        {
            Arguments = $"--postgres {_postgres.Port}",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.Npgsql), MemberType = typeof(LibraryVersion))]
    public void DataSourceCommandEmitsSingleProviderSpan(string packageVersion)
    {
        EnableBytecodeInstrumentation();

        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        // Npgsql 10 also emits a physical-connection span; count query spans separately.
        Func<Span, bool> isCommandSpan = span => span.Attributes.Any(attribute =>
            (attribute.Key is "db.statement" or "db.query.text") && attribute.Value.StringValue == "SELECT 123");
        collector.Expect("TestApplication.Npgsql", span => span.Name == "DataSource.parent");
        collector.Expect("Npgsql", isCommandSpan);
        collector.Expect("TestApplication.Npgsql", span => span.Name == "DataSource.subsequent");
        collector.ExpectAllCollected(collected =>
        {
            Assert.DoesNotContain(collected, item => item.Scope.Name == "OpenTelemetry.AutoInstrumentation.AdoNet");
            var parent = Assert.Single(collected, item => item.Span.Name == "DataSource.parent").Span;
            var provider = Assert.Single(collected, item => item.Scope.Name == "Npgsql" && isCommandSpan(item.Span)).Span;
            var subsequent = Assert.Single(collected, item => item.Span.Name == "DataSource.subsequent").Span;

            Assert.Equal(parent.TraceId, provider.TraceId);
            Assert.Equal(parent.SpanId, provider.ParentSpanId);
            Assert.Equal(parent.TraceId, subsequent.TraceId);
            Assert.Equal(parent.SpanId, subsequent.ParentSpanId);
            return true;
        });

        RunTestApplication(new()
        {
            Arguments = $"--postgres {_postgres.Port} --db-data-source-scenario",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.Npgsql), MemberType = typeof(LibraryVersion))]
    public void PropagatesTraceContext(string packageVersion)
    {
        EnableBytecodeInstrumentation();
        SetEnvironmentVariable(NpgsqlTraceContextPropagationTestHelper.ContextPropagationEnvVar, bool.TrueString);

        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        var (standardOutput, _, _) = RunTestApplication(new()
        {
            Arguments = $"--postgres {_postgres.Port} --context-propagation-scenario",
            PackageVersion = packageVersion
        });

        var expectedCount = NpgsqlTraceContextPropagationTestHelper.SupportsCopyTracing(packageVersion) ? 7 : 6;
        var applicationNames = NpgsqlTraceContextPropagationTestHelper.ExtractApplicationNames(standardOutput, expectedCount);
        var postOperationApplicationNames = NpgsqlTraceContextPropagationTestHelper.ExtractValues(standardOutput, "ContextPostOperationApplicationName=");
        Assert.Equal([ContextPropagationApplicationName], postOperationApplicationNames);
        foreach (var applicationName in applicationNames)
        {
            collector.Expect(
                NpgsqlTraceContextPropagationTestHelper.ScopeName,
                span => NpgsqlTraceContextPropagationTestHelper.MatchesApplicationName(span, applicationName),
                "Npgsql span matching the traceparent propagated through application_name.");
        }

        collector.ExpectAllCollected(
            collected => collected.Count(item =>
                item.Scope.Name == NpgsqlTraceContextPropagationTestHelper.ScopeName &&
                applicationNames.Any(applicationName => NpgsqlTraceContextPropagationTestHelper.MatchesApplicationName(item.Span, applicationName))) == applicationNames.Count);
        collector.AssertExpectations();
    }

#if NET
    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(NpgsqlWithTracingFilters))]
    public void ClearsStaleTraceContext(string packageVersion)
    {
        EnableBytecodeInstrumentation();
        SetEnvironmentVariable(NpgsqlTraceContextPropagationTestHelper.ContextPropagationEnvVar, bool.TrueString);

        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        var (standardOutput, _, _) = RunTestApplication(new()
        {
            Arguments = $"--postgres {_postgres.Port} --stale-context-scenario",
            PackageVersion = packageVersion
        });

        var tracedApplicationNames = NpgsqlTraceContextPropagationTestHelper.ExtractValues(standardOutput, "StaleTracedApplicationName=");
        var postOperationApplicationNames = NpgsqlTraceContextPropagationTestHelper.ExtractValues(standardOutput, "StalePostOperationApplicationName=");
        var untracedApplicationNames = NpgsqlTraceContextPropagationTestHelper.ExtractValues(standardOutput, "StaleUntracedApplicationName=");
        var tracedBackendProcessIds = NpgsqlTraceContextPropagationTestHelper.ExtractValues(standardOutput, "StaleTracedBackendProcessId=");
        var untracedBackendProcessIds = NpgsqlTraceContextPropagationTestHelper.ExtractValues(standardOutput, "StaleUntracedBackendProcessId=");
        Assert.Single(tracedApplicationNames);
        NpgsqlTraceContextPropagationTestHelper.AssertAllTraceParents(tracedApplicationNames);
        Assert.Equal([StaleContextApplicationName], postOperationApplicationNames);
        Assert.Equal([StaleContextApplicationName], untracedApplicationNames);
        Assert.Single(tracedBackendProcessIds);
        Assert.Equal(tracedBackendProcessIds, untracedBackendProcessIds);

        var failedTransactionApplicationNames = NpgsqlTraceContextPropagationTestHelper.ExtractValues(standardOutput, "FailedTransactionUntracedApplicationName=");
        var failedTransactionPostRollbackApplicationNames = NpgsqlTraceContextPropagationTestHelper.ExtractValues(standardOutput, "FailedTransactionPostRollbackApplicationName=");
        var failedTransactionBackendProcessIds = NpgsqlTraceContextPropagationTestHelper.ExtractValues(standardOutput, "FailedTransactionBackendProcessId=");
        var failedTransactionUntracedBackendProcessIds = NpgsqlTraceContextPropagationTestHelper.ExtractValues(standardOutput, "FailedTransactionUntracedBackendProcessId=");
        Assert.Equal([StaleContextApplicationName], failedTransactionApplicationNames);
        Assert.Equal([StaleContextApplicationName], failedTransactionPostRollbackApplicationNames);
        Assert.Single(failedTransactionBackendProcessIds);
        Assert.Equal(failedTransactionBackendProcessIds, failedTransactionUntracedBackendProcessIds);
        Assert.Equal(tracedBackendProcessIds, failedTransactionBackendProcessIds);

        if (NpgsqlTraceContextPropagationTestHelper.SupportsCopyTracing(packageVersion))
        {
            var tracedCopyApplicationNames = NpgsqlTraceContextPropagationTestHelper.ExtractValues(standardOutput, "StaleTracedCopyApplicationName=");
            var postCopyApplicationNames = NpgsqlTraceContextPropagationTestHelper.ExtractValues(standardOutput, "StalePostCopyApplicationName=");
            var untracedCopyApplicationNames = NpgsqlTraceContextPropagationTestHelper.ExtractValues(standardOutput, "StaleUntracedCopyApplicationName=");
            var tracedCopyBackendProcessIds = NpgsqlTraceContextPropagationTestHelper.ExtractValues(standardOutput, "StaleTracedCopyBackendProcessId=");
            var untracedCopyBackendProcessIds = NpgsqlTraceContextPropagationTestHelper.ExtractValues(standardOutput, "StaleUntracedCopyBackendProcessId=");
            Assert.Single(tracedCopyApplicationNames);
            NpgsqlTraceContextPropagationTestHelper.AssertAllTraceParents(tracedCopyApplicationNames);
            Assert.Equal([StaleContextApplicationName], postCopyApplicationNames);
            Assert.Equal([StaleContextApplicationName], untracedCopyApplicationNames);
            Assert.Single(tracedCopyBackendProcessIds);
            Assert.Equal(tracedCopyBackendProcessIds, untracedCopyBackendProcessIds);
            Assert.Equal(tracedBackendProcessIds, tracedCopyBackendProcessIds);
            tracedApplicationNames = tracedApplicationNames.Concat(tracedCopyApplicationNames).ToList();
        }

        foreach (var applicationName in tracedApplicationNames)
        {
            collector.Expect(
                NpgsqlTraceContextPropagationTestHelper.ScopeName,
                span => NpgsqlTraceContextPropagationTestHelper.MatchesApplicationName(span, applicationName),
                "Npgsql span matching the traceparent propagated through application_name.");
        }

        collector.AssertExpectations();
    }
#endif

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.Npgsql), MemberType = typeof(LibraryVersion))]
    public void SkipsTraceContextPropagationForMultiplexing(string packageVersion)
    {
        EnableBytecodeInstrumentation();
        SetEnvironmentVariable(NpgsqlTraceContextPropagationTestHelper.ContextPropagationEnvVar, bool.TrueString);

        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        var (standardOutput, _, _) = RunTestApplication(new()
        {
            Arguments = $"--postgres {_postgres.Port} --multiplexing-context-scenario",
            PackageVersion = packageVersion
        });

        var applicationNames = NpgsqlTraceContextPropagationTestHelper.ExtractValues(standardOutput, "MultiplexedApplicationName=");
        Assert.Equal(20, applicationNames.Count);
        Assert.All(applicationNames, applicationName => Assert.Equal(MultiplexingApplicationName, applicationName));
    }

#if NET
    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.Npgsql), MemberType = typeof(LibraryVersion))]
    public void SubmitMetrics(string packageVersion)
    {
        using var collector = new MockMetricsCollector(Output);
        SetExporter(collector);
        collector.Expect("Npgsql");

        RunTestApplication(new()
        {
            Arguments = $"--postgres {_postgres.Port}",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }
#endif
}
