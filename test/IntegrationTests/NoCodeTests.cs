// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

public class NoCodeTests : TestHelper
{
    public NoCodeTests(ITestOutputHelper output)
        : base("NoCode", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitsTraces()
    {
        EnableBytecodeInstrumentation();
        EnableFileBasedConfig();
        using var collector = new MockSpansCollector(Output);
        SetFileBasedExporter(collector);

        List<KeyValue> allTypeOfAttributes = [
            new() { Key = "attribute_key_string", Value = new AnyValue { StringValue = "string_value" } },
            new() { Key = "attribute_key_bool", Value = new AnyValue { BoolValue = true } },
            new() { Key = "attribute_key_int", Value = new AnyValue { IntValue = 12345 } },
            new() { Key = "attribute_key_double", Value = new AnyValue { DoubleValue = 123.45 } },
            new() { Key = "attribute_key_string_array", Value = new AnyValue { ArrayValue = new ArrayValue { Values = { new AnyValue { StringValue = "value1" }, new AnyValue { StringValue = "value2" }, new AnyValue { StringValue = "value3" } } } } },
            new() { Key = "attribute_key_bool_array", Value = new AnyValue { ArrayValue = new ArrayValue { Values = { new AnyValue { BoolValue = true }, new AnyValue { BoolValue = false }, new AnyValue { BoolValue = true } } } } },
            new() { Key = "attribute_key_int_array", Value = new AnyValue { ArrayValue = new ArrayValue { Values = { new AnyValue { IntValue = 123 }, new AnyValue { IntValue = 456 }, new AnyValue { IntValue = 789 } } } } },
            new() { Key = "attribute_key_double_array", Value = new AnyValue { ArrayValue = new ArrayValue { Values = { new AnyValue { DoubleValue = 123.45 }, new AnyValue { DoubleValue = 678.90 } } } } },
        ];

        collector.ExpectNoCode("Span-TestMethodStatic", Span.Types.SpanKind.Internal, allTypeOfAttributes);
        collector.ExpectNoCode("Span-TestMethod0", Span.Types.SpanKind.Client);
        collector.ExpectNoCode("Span-TestMethodA", Span.Types.SpanKind.Consumer);
        collector.ExpectNoCode("Span-TestMethod1String", Span.Types.SpanKind.Producer);
        collector.ExpectNoCode("Span-TestMethod1Int", Span.Types.SpanKind.Server);
        collector.ExpectNoCode("Span-TestMethod2");
        collector.ExpectNoCode("Span-TestMethod3");
        collector.ExpectNoCode("Span-TestMethod4");
        collector.ExpectNoCode("Span-TestMethod5");
        collector.ExpectNoCode("Span-TestMethod6");
        collector.ExpectNoCode("Span-TestMethod7");
        collector.ExpectNoCode("Span-TestMethod8");
        collector.ExpectNoCode("Span-TestMethod9");

        collector.ExpectNoCode("Span-ReturningTestMethodStatic");
        collector.ExpectNoCode("Span-ReturningTestMethod0");
        collector.ExpectNoCode("Span-ReturningStringTestMethod");
        collector.ExpectNoCode("Span-ReturningCustomClassTestMethod");
        collector.ExpectNoCode("Span-ReturningTestMethod1String");
        collector.ExpectNoCode("Span-ReturningTestMethod1Int");
        collector.ExpectNoCode("Span-ReturningTestMethod2");
        collector.ExpectNoCode("Span-ReturningTestMethod3");
        collector.ExpectNoCode("Span-ReturningTestMethod4");
        collector.ExpectNoCode("Span-ReturningTestMethod5");
        collector.ExpectNoCode("Span-ReturningTestMethod6");
        collector.ExpectNoCode("Span-ReturningTestMethod7");
        collector.ExpectNoCode("Span-ReturningTestMethod8");
        collector.ExpectNoCode("Span-ReturningTestMethod9");

        // TagList natively supports up to 9 attributes in the performant way, so we need to verify that more than 9 attributes are supported
        List<KeyValue> moreThan9Attributes =
        [
            new() { Key = "attribute_key_string0", Value = new AnyValue { StringValue = "string_value0" } },
            new() { Key = "attribute_key_string1", Value = new AnyValue { StringValue = "string_value1" } },
            new() { Key = "attribute_key_string2", Value = new AnyValue { StringValue = "string_value2" } },
            new() { Key = "attribute_key_string3", Value = new AnyValue { StringValue = "string_value3" } },
            new() { Key = "attribute_key_string4", Value = new AnyValue { StringValue = "string_value4" } },
            new() { Key = "attribute_key_string5", Value = new AnyValue { StringValue = "string_value5" } },
            new() { Key = "attribute_key_string6", Value = new AnyValue { StringValue = "string_value6" } },
            new() { Key = "attribute_key_string7", Value = new AnyValue { StringValue = "string_value7" } },
            new() { Key = "attribute_key_string8", Value = new AnyValue { StringValue = "string_value8" } },
            new() { Key = "attribute_key_string9", Value = new AnyValue { StringValue = "string_value9" } },
        ];

        collector.ExpectAsyncNoCode("Span-TestMethodStaticAsync", Span.Types.SpanKind.Internal, moreThan9Attributes);
        collector.ExpectAsyncNoCode("Span-TestMethod0Async");
        collector.ExpectAsyncNoCode("Span-TestMethodAAsync");
        collector.ExpectAsyncNoCode("Span-TestMethod1StringAsync");
        collector.ExpectAsyncNoCode("Span-TestMethod1IntAsync");
        collector.ExpectAsyncNoCode("Span-TestMethod2Async");
        collector.ExpectAsyncNoCode("Span-TestMethod3Async");
        collector.ExpectAsyncNoCode("Span-TestMethod4Async");
        collector.ExpectAsyncNoCode("Span-TestMethod5Async");
        collector.ExpectAsyncNoCode("Span-TestMethod6Async");
        collector.ExpectAsyncNoCode("Span-TestMethod7Async");
        collector.ExpectAsyncNoCode("Span-TestMethod8Async");
        collector.ExpectAsyncNoCode("Span-TestMethod9Async");

        collector.ExpectAsyncNoCode("Span-IntTaskTestMethodAsync");
#if NET
        collector.ExpectAsyncNoCode("Span-ValueTaskTestMethodAsync");
        collector.ExpectAsyncNoCode("Span-IntValueTaskTestMethodAsync");
#endif

        collector.ExpectNoCode("Span-GenericTestMethod");
        collector.ExpectAsyncNoCode("Span-GenericTestMethodAsync");
        collector.ExpectNoCode("Span-GenericTestMethodWithParameters");

        // Dynamic attribute tests - extracting values from method parameters
        List<KeyValue> processOrderAttributes =
        [
            new() { Key = "order.id", Value = new AnyValue { StringValue = "ORD-12345" } },
            new() { Key = "order.quantity", Value = new AnyValue { IntValue = 5 } },
        ];
        collector.ExpectNoCode("Span-ProcessOrder", Span.Types.SpanKind.Internal, processOrderAttributes);

        List<KeyValue> processCustomerAttributes =
        [
            new() { Key = "customer.id", Value = new AnyValue { StringValue = "CUST-001" } },
            new() { Key = "customer.name", Value = new AnyValue { StringValue = "John Doe" } },
            new() { Key = "customer.email", Value = new AnyValue { StringValue = "john@example.com" } },
            new() { Key = "customer.city", Value = new AnyValue { StringValue = "Seattle" } },
            new() { Key = "customer.country", Value = new AnyValue { StringValue = "USA" } },
        ];
        collector.ExpectNoCode("Span-ProcessCustomer", Span.Types.SpanKind.Internal, processCustomerAttributes);

        List<KeyValue> auditActionAttributes =
        [
            new() { Key = "action", Value = new AnyValue { StringValue = "user_login" } },
            new() { Key = "service.name", Value = new AnyValue { StringValue = "TestService" } },
            new() { Key = "merchant.id", Value = new AnyValue { IntValue = 12345 } },
        ];
        collector.ExpectNoCode("Span-AuditAction", Span.Types.SpanKind.Internal, auditActionAttributes);

        List<KeyValue> createResourceAttributes =
        [
            new() { Key = "resource.full_id", Value = new AnyValue { StringValue = "database/db-prod-001" } },
        ];
        collector.ExpectNoCode("Span-CreateResource", Span.Types.SpanKind.Internal, createResourceAttributes);

        List<KeyValue> processWithDefaultAttributes =
        [
            new() { Key = "value", Value = new AnyValue { StringValue = "default_value" } },
        ];
        collector.ExpectNoCode("Span-ProcessWithDefault", Span.Types.SpanKind.Internal, processWithDefaultAttributes);

        List<KeyValue> operationWithMetadataAttributes =
        [
            new() { Key = "method.name", Value = new AnyValue { StringValue = "OperationWithMetadata" } },
            new() { Key = "type.name", Value = new AnyValue { StringValue = "TestApplication.NoCode.DynamicAttributeTestingClass" } },
            new() { Key = "operation.full_name", Value = new AnyValue { StringValue = "TestApplication.NoCode.DynamicAttributeTestingClass.OperationWithMetadata" } },
        ];
        collector.ExpectNoCode("Span-OperationWithMetadata", Span.Types.SpanKind.Internal, operationWithMetadataAttributes);

        List<KeyValue> processOrderAsyncAttributes =
        [
            new() { Key = "order.id", Value = new AnyValue { StringValue = "ORD-99999" } },
            new() { Key = "order.amount", Value = new AnyValue { DoubleValue = 199.99 } },
            new() { Key = "order.currency", Value = new AnyValue { StringValue = "USD" } },
        ];
        collector.ExpectAsyncNoCode("Span-ProcessOrderAsync", Span.Types.SpanKind.Internal, processOrderAsyncAttributes);

        List<KeyValue> completeOrderAttributes =
        [
            new() { Key = "order.id", Value = new AnyValue { StringValue = "ORD-COMPLETE" } },
        ];
        collector.ExpectNoCode("Span-CompleteOrder", Span.Types.SpanKind.Internal, completeOrderAttributes);

        // Dynamic span name tests
        List<KeyValue> processTransactionAttributes =
        [
            new() { Key = "transaction.id", Value = new AnyValue { StringValue = "TXN-12345" } },
            new() { Key = "transaction.type", Value = new AnyValue { StringValue = "payment" } },
        ];
        collector.ExpectNoCode("Transaction-payment-TXN-12345", Span.Types.SpanKind.Internal, processTransactionAttributes);

        List<KeyValue> executeQueryAttributes =
        [
            new() { Key = "db.name", Value = new AnyValue { StringValue = "ProductionDB" } },
            new() { Key = "db.table", Value = new AnyValue { StringValue = "users" } },
        ];
        collector.ExpectNoCode("Query.ProductionDB.users", Span.Types.SpanKind.Internal, executeQueryAttributes);

        RunTestApplication();

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void MalformedConfiguration_ComprehensiveTest()
    {
        EnableBytecodeInstrumentation();
        EnableFileBasedConfig("config-malformed.yaml");
        using var collector = new MockSpansCollector(Output);
        SetFileBasedExporter(collector);

        collector.ExpectNoCode("Span-Valid-Basic", Span.Types.SpanKind.Client);

        List<KeyValue> validWithInvalidDynamicAttr =
        [
            new() { Key = "valid_static_attr", Value = new AnyValue { StringValue = "static_value" } },
        ];
        collector.ExpectNoCode("Span-Valid-WithInvalidDynamicAttribute", Span.Types.SpanKind.Internal, validWithInvalidDynamicAttr);

        collector.ExpectNoCode("Span-Valid-WithInvalidStatusRule", Span.Types.SpanKind.Server);

        collector.ExpectNoCode("Span-Valid-StaticNameFallback", Span.Types.SpanKind.Producer);

        List<KeyValue> validStaticSpanAttr =
        [
            new() { Key = "test_marker", Value = new AnyValue { StringValue = "malformed_test" } },
        ];
        collector.ExpectNoCode("Span-Valid-Static", Span.Types.SpanKind.Internal, validStaticSpanAttr);

        RunTestApplication();

        collector.AssertExpectations();
        // All invalid configurations should be silently skipped. It means that there is no more spans.
        collector.AssertEmpty();
    }
}

file static class NoCodeMockSpansCollectorExtensions
{
    public static void ExpectNoCode(this MockSpansCollector collector, string expectedSpanName, Span.Types.SpanKind expectedSpanKind = Span.Types.SpanKind.Internal, List<KeyValue>? expectedAttributes = null)
    {
        collector.ExpectNoCode(AssertSpan, expectedSpanName, expectedSpanKind, expectedAttributes);
    }

    public static void ExpectAsyncNoCode(this MockSpansCollector collector, string expectedSpanName, Span.Types.SpanKind expectedSpanKind = Span.Types.SpanKind.Internal, List<KeyValue>? expectedAttributes = null)
    {
        collector.ExpectNoCode(AssertAsyncSpan, expectedSpanName, expectedSpanKind, expectedAttributes);
    }

    private static void ExpectNoCode(this MockSpansCollector collector, Func<Span, string, Span.Types.SpanKind, List<KeyValue>?, bool> assert, string expectedSpanName, Span.Types.SpanKind expectedSpanKind, List<KeyValue>? expectedAttributes)
    {
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", VersionHelper.AutoInstrumentationVersion, x => assert(x, expectedSpanName, expectedSpanKind, expectedAttributes), GetSpanDescription(expectedSpanName, expectedSpanKind, expectedAttributes));
    }

    private static string GetSpanDescription(string expectedSpanName, Span.Types.SpanKind expectedSpanKind, List<KeyValue>? expectedAttributes)
    {
        return $"Instrumentation Scope Name: 'OpenTelemetry.AutoInstrumentation.NoCode', Span Name: '{expectedSpanName}', Span Kind: '{expectedSpanKind}', Attributes: '{(expectedAttributes != null ? string.Join(", ", expectedAttributes.Select(attr => $"{attr.Key}={attr.Value}")) : "<none>")}'";
    }

    private static bool AssertSpan(Span span, string expectedSpanName, Span.Types.SpanKind expectedSpanKind, List<KeyValue>? expectedAttributes)
    {
        expectedAttributes ??= [];

        return expectedSpanName == span.Name && expectedSpanKind == span.Kind && expectedAttributes.SequenceEqual(span.Attributes);
    }

    private static bool AssertAsyncSpan(Span span, string expectedSpanName, Span.Types.SpanKind expectedSpanKind, List<KeyValue>? expectedAttributes)
    {
        return AssertSpan(span, expectedSpanName, expectedSpanKind, expectedAttributes) && AssertSpanDuration(span);
    }

    private static bool AssertSpanDuration(Span span)
    {
        var ticks = (long)((span.EndTimeUnixNano - span.StartTimeUnixNano) / 100); // 100ns = 1 tick

        var duration = TimeSpan.FromTicks(ticks);

        // All async methods have a 100ms delay, need to be a bit lower (due to timer resolution).
        // Decrease by a margin of a default timer resolution on Windows (~16ms) to avoid flaky tests.
        return duration > TimeSpan.FromMilliseconds(84);
    }
}
