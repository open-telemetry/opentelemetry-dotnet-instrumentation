// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;

namespace IntegrationTests;

public class AdoNetStubTests(ITestOutputHelper output) : TestHelper("AdoNetStub", output)
{
#if NET
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Category", "EndToEnd")]
    public void DbDataSourceCommandsPreserveParentActivity(bool completeSynchronously)
    {
        EnableBytecodeInstrumentation();

        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        string[] operations = ["ExecuteScalarAsync", "ExecuteNonQueryAsync", "ExecuteReaderAsync"];
        foreach (var operation in operations)
        {
            collector.Expect("TestApplication.AdoNetStub", span => span.Name == $"{operation}.parent");
            collector.Expect("TestApplication.AdoNetStub", span => span.Name == $"{operation}.subsequent");
            collector.ExpectAdoNet(
                "SELECT FakeTable",
                [
                    new() { Key = "db.system.name", Value = new AnyValue { StringValue = "other_sql" } },
                    new() { Key = "db.query.summary", Value = new AnyValue { StringValue = "SELECT FakeTable" } },
                    new() { Key = "db.query.text", Value = new AnyValue { StringValue = "SELECT * FROM FakeTable" } }
                ]);
        }

        collector.ExpectAllCollected(collected =>
        {
            Assert.Equal(9, collected.Count);
            foreach (var operation in operations)
            {
                var parent = Assert.Single(collected, item => item.Span.Name == $"{operation}.parent").Span;
                var subsequent = Assert.Single(collected, item => item.Span.Name == $"{operation}.subsequent").Span;
                var database = Assert.Single(collected, item =>
                    item.Scope.Name == "OpenTelemetry.AutoInstrumentation.AdoNet" && item.Span.ParentSpanId == parent.SpanId).Span;

                Assert.Equal(parent.TraceId, database.TraceId);
                Assert.Equal(parent.TraceId, subsequent.TraceId);
                Assert.Equal(parent.SpanId, subsequent.ParentSpanId);
            }

            return true;
        });

        RunTestApplication(new() { Arguments = $"--db-data-source {completeSynchronously}" });

        collector.AssertExpectations();
    }
#endif

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
