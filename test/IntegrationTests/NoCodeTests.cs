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
    public async Task SubmitsTraces()
    {
        EnableBytecodeInstrumentation();
        EnableFileBasedConfigWithDefaultPath();
        using var collector = await MockSpansCollector.InitializeAsync(Output);
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

        RunTestApplication();

        collector.AssertExpectations();
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
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => assert(x, expectedSpanName, expectedSpanKind, expectedAttributes), GetSpanDescription(expectedSpanName, expectedSpanKind, expectedAttributes));
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

        return duration > TimeSpan.FromMilliseconds(98); // all async methods have a 100ms delay, need to be a bit lower (due to timer resolution)
    }
}
