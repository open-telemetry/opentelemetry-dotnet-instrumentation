// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

public class SqliteMicrosoftTests(ITestOutputHelper output) : TestHelper("Sqlite.Microsoft", output)
{
    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.SqliteMicrosoft), MemberType = typeof(LibraryVersion))]
    public void SubmitTraces(string packageVersion)
    {
        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADONET_INSTRUMENTATION_ENABLED", "false");

        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        // Setup span: CREATE TABLE emitted by ExecuteNonQueryAsync on the in-memory connection
        collector.ExpectAdoNet(
            "CREATE TABLE MyTable",
            [
                new() { Key = "db.system.name", Value = new AnyValue { StringValue = "sqlite" } },
                new() { Key = "db.query.summary", Value = new AnyValue { StringValue = "CREATE TABLE MyTable" } },
                new() { Key = "db.query.text", Value = new AnyValue { StringValue = "CREATE TABLE MyTable (Id INTEGER PRIMARY KEY, Value TEXT)" } },
                new() { Key = "db.namespace", Value = new AnyValue { StringValue = "main" } }
            ]);

        // Sync spans
        collector.ExpectAdoNet(
            "INSERT MyTable",
            [
                new() { Key = "db.system.name", Value = new AnyValue { StringValue = "sqlite" } },
                new() { Key = "db.query.summary", Value = new AnyValue { StringValue = "INSERT MyTable" } },
                new() { Key = "db.query.text", Value = new AnyValue { StringValue = "INSERT INTO MyTable VALUES (?, ?)" } },
                new() { Key = "db.namespace", Value = new AnyValue { StringValue = "main" } }
            ]);
        collector.ExpectAdoNet(
            "SELECT MyTable",
            [
                new() { Key = "db.system.name", Value = new AnyValue { StringValue = "sqlite" } },
                new() { Key = "db.query.summary", Value = new AnyValue { StringValue = "SELECT MyTable" } },
                new() { Key = "db.query.text", Value = new AnyValue { StringValue = "SELECT COUNT(*) FROM MyTable" } },
                new() { Key = "db.namespace", Value = new AnyValue { StringValue = "main" } }
            ]);
        collector.ExpectAdoNet(
            "SELECT MyTable",
            [
                new() { Key = "db.system.name", Value = new AnyValue { StringValue = "sqlite" } },
                new() { Key = "db.query.summary", Value = new AnyValue { StringValue = "SELECT MyTable" } },
                new() { Key = "db.query.text", Value = new AnyValue { StringValue = "SELECT * FROM MyTable" } },
                new() { Key = "db.namespace", Value = new AnyValue { StringValue = "main" } }
            ]);

        // Async spans
        collector.ExpectAdoNet(
            "INSERT MyTable",
            [
                new() { Key = "db.system.name", Value = new AnyValue { StringValue = "sqlite" } },
                new() { Key = "db.query.summary", Value = new AnyValue { StringValue = "INSERT MyTable" } },
                new() { Key = "db.query.text", Value = new AnyValue { StringValue = "INSERT INTO MyTable VALUES (?, ?)" } },
                new() { Key = "db.namespace", Value = new AnyValue { StringValue = "main" } }
            ]);
        collector.ExpectAdoNet(
            "SELECT MyTable",
            [
                new() { Key = "db.system.name", Value = new AnyValue { StringValue = "sqlite" } },
                new() { Key = "db.query.summary", Value = new AnyValue { StringValue = "SELECT MyTable" } },
                new() { Key = "db.query.text", Value = new AnyValue { StringValue = "SELECT COUNT(*) FROM MyTable" } },
                new() { Key = "db.namespace", Value = new AnyValue { StringValue = "main" } }
            ]);
        collector.ExpectAdoNet(
            "SELECT MyTable",
            [
                new() { Key = "db.system.name", Value = new AnyValue { StringValue = "sqlite" } },
                new() { Key = "db.query.summary", Value = new AnyValue { StringValue = "SELECT MyTable" } },
                new() { Key = "db.query.text", Value = new AnyValue { StringValue = "SELECT * FROM MyTable" } },
                new() { Key = "db.namespace", Value = new AnyValue { StringValue = "main" } }
            ]);

        RunTestApplication(new()
        {
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
        collector.AssertEmpty();
    }
}

file static class AdoNetMockSpansCollectorExtensions
{
    public static void ExpectAdoNet(this MockSpansCollector collector, string expectedSpanName, List<KeyValue>? expectedAttributes = null)
    {
        const string expectedScopeName = "OpenTelemetry.AutoInstrumentation.AdoNet";
        const string expectedSchemaUrl = "https://opentelemetry.io/schemas/1.40.0";
        collector.Expect(expectedScopeName, null, x => AssertSpan(x, expectedSpanName, expectedAttributes), GetSpanDescription(expectedSpanName, expectedAttributes), expectedSchemaUrl);
    }

    private static string GetSpanDescription(string expectedSpanName, List<KeyValue>? expectedAttributes)
    {
        return $"Instrumentation Scope Name: 'OpenTelemetry.AutoInstrumentation.AdoNet', Span Name: '{expectedSpanName}', Attributes: '{(expectedAttributes != null ? string.Join(", ", expectedAttributes.Select(attr => $"{attr.Key}={attr.Value}")) : "<none>")}'";
    }

    private static bool AssertSpan(Span span, string expectedSpanName, List<KeyValue>? expectedAttributes)
    {
        expectedAttributes ??= [];

        if (expectedSpanName != span.Name || Span.Types.SpanKind.Client != span.Kind)
        {
            return false;
        }

        return expectedAttributes.SequenceEqual(span.Attributes);
    }
}
