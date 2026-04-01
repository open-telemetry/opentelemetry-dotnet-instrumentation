// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

public class AdoNetStubTests(ITestOutputHelper output) : TestHelper("AdoNetStub", output)
{
    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    public void SubmitTraces()
    {
        EnableBytecodeInstrumentation();

        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        collector.ExpectAdoNet(
            "INSERT FakeTable",
            [

                new() { Key = "db.system.name", Value = new AnyValue { StringValue = "other_sql" } },
                new() { Key = "db.query.summary", Value = new AnyValue { StringValue = "INSERT FakeTable" } },
                new() { Key = "db.query.text", Value = new AnyValue { StringValue = "INSERT INTO FakeTable VALUES (?, ?)" } },
                new() { Key = "db.namespace", Value = new AnyValue { StringValue = "FakeDatabase" } },
                new() { Key = "server.address", Value = new AnyValue { StringValue = "FakeServer" } },
                new() { Key = "server.port", Value = new AnyValue { IntValue = 5433 } }
            ]);
        collector.ExpectAdoNet(
            "SELECT FakeTable",
            [
                new() { Key = "db.system.name", Value = new AnyValue { StringValue = "other_sql" } },
                new() { Key = "db.query.summary", Value = new AnyValue { StringValue = "SELECT FakeTable" } },
                new() { Key = "db.query.text", Value = new AnyValue { StringValue = "SELECT COUNT(*) FROM FakeTable" } },
                new() { Key = "db.namespace", Value = new AnyValue { StringValue = "FakeDatabase" } },
                new() { Key = "server.address", Value = new AnyValue { StringValue = "FakeServer" } },
                new() { Key = "server.port", Value = new AnyValue { IntValue = 5433 } }
                ]);
        collector.ExpectAdoNet(
            "SELECT FakeTable",
            [
                new() { Key = "db.system.name", Value = new AnyValue { StringValue = "other_sql" } },
                new() { Key = "db.query.summary", Value = new AnyValue { StringValue = "SELECT FakeTable" } },
                new() { Key = "db.query.text", Value = new AnyValue { StringValue = "SELECT * FROM FakeTable" } },
                new() { Key = "db.namespace", Value = new AnyValue { StringValue = "FakeDatabase" } },
                new() { Key = "server.address", Value = new AnyValue { StringValue = "FakeServer" } },
                new() { Key = "server.port", Value = new AnyValue { IntValue = 5433 } }
                ]);
        collector.ExpectAdoNet(
            "INSERT FakeTable",
            [
                new() { Key = "db.system.name", Value = new AnyValue { StringValue = "other_sql" } },
                new() { Key = "db.query.summary", Value = new AnyValue { StringValue = "INSERT FakeTable" } },
                new() { Key = "db.query.text", Value = new AnyValue { StringValue = "INSERT INTO FakeTable VALUES (?, ?)" } },
                new() { Key = "db.namespace", Value = new AnyValue { StringValue = "FakeDatabase" } },
                new() { Key = "server.address", Value = new AnyValue { StringValue = "FakeServer" } },
                new() { Key = "server.port", Value = new AnyValue { IntValue = 5433 } }
                ]);
        collector.ExpectAdoNet(
            "SELECT FakeTable",
            [
                new() { Key = "db.system.name", Value = new AnyValue { StringValue = "other_sql" } },
                new() { Key = "db.query.summary", Value = new AnyValue { StringValue = "SELECT FakeTable" } },
                new() { Key = "db.query.text", Value = new AnyValue { StringValue = "SELECT COUNT(*) FROM FakeTable" } },
                new() { Key = "db.namespace", Value = new AnyValue { StringValue = "FakeDatabase" } },
                new() { Key = "server.address", Value = new AnyValue { StringValue = "FakeServer" } },
                new() { Key = "server.port", Value = new AnyValue { IntValue = 5433 } }
                ]);
        collector.ExpectAdoNet(
            "SELECT FakeTable",
            [
                new() { Key = "db.system.name", Value = new AnyValue { StringValue = "other_sql" } },
                new() { Key = "db.query.summary", Value = new AnyValue { StringValue = "SELECT FakeTable" } },
                new() { Key = "db.query.text", Value = new AnyValue { StringValue = "SELECT * FROM FakeTable" } },
                new() { Key = "db.namespace", Value = new AnyValue { StringValue = "FakeDatabase" } },
                new() { Key = "server.address", Value = new AnyValue { StringValue = "FakeServer" } },
                new() { Key = "server.port", Value = new AnyValue { IntValue = 5433 } }
                ]);

        RunTestApplication(new() { });

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
