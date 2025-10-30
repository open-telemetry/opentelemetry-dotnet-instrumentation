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
        EnableFileBasedConfigWithDefaultPath();
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

        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-TestMethodStatic", Span.Types.SpanKind.Internal, allTypeOfAttributes));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-TestMethod0", Span.Types.SpanKind.Client));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-TestMethodA", Span.Types.SpanKind.Consumer));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-TestMethod1String", Span.Types.SpanKind.Producer));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-TestMethod1Int", Span.Types.SpanKind.Server));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-TestMethod2"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-TestMethod3"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-TestMethod4"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-TestMethod5"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-TestMethod6"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-TestMethod7"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-TestMethod8"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-TestMethod9"));

        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-ReturningTestMethodStatic"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-ReturningTestMethod0"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-ReturningStringTestMethod"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-ReturningCustomClassTestMethod"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-ReturningTestMethod1String"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-ReturningTestMethod1Int"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-ReturningTestMethod2"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-ReturningTestMethod3"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-ReturningTestMethod4"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-ReturningTestMethod5"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-ReturningTestMethod6"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-ReturningTestMethod7"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-ReturningTestMethod8"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-ReturningTestMethod9"));

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

        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertAsyncSpan(x, "Span-TestMethodStaticAsync", Span.Types.SpanKind.Internal, moreThan9Attributes));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertAsyncSpan(x, "Span-TestMethod0Async"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertAsyncSpan(x, "Span-TestMethodAAsync"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertAsyncSpan(x, "Span-TestMethod1StringAsync"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertAsyncSpan(x, "Span-TestMethod1IntAsync"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertAsyncSpan(x, "Span-TestMethod2Async"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertAsyncSpan(x, "Span-TestMethod3Async"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertAsyncSpan(x, "Span-TestMethod4Async"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertAsyncSpan(x, "Span-TestMethod5Async"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertAsyncSpan(x, "Span-TestMethod6Async"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertAsyncSpan(x, "Span-TestMethod7Async"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertAsyncSpan(x, "Span-TestMethod8Async"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertAsyncSpan(x, "Span-TestMethod9Async"));

        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertAsyncSpan(x, "Span-IntTaskTestMethodAsync"));
#if NET
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertAsyncSpan(x, "Span-ValueTaskTestMethodAsync"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertAsyncSpan(x, "Span-IntValueTaskTestMethodAsync"));
#endif

        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-GenericTestMethod"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertAsyncSpan(x, "Span-GenericTestMethodAsync"));

        RunTestApplication();

        collector.AssertExpectations();
    }

    private bool AssertSpan(Span span, string expectedSpanName, Span.Types.SpanKind expectedSpanKind = Span.Types.SpanKind.Internal, List<KeyValue>? expectedAttributes = null)
    {
        expectedAttributes ??= [];

        return expectedSpanName == span.Name && expectedSpanKind == span.Kind && expectedAttributes.SequenceEqual(span.Attributes);
    }

    private bool AssertAsyncSpan(Span span, string expectedSpanName, Span.Types.SpanKind expectedSpanKind = Span.Types.SpanKind.Internal, List<KeyValue>? expectedAttributes = null)
    {
        return AssertSpan(span, expectedSpanName, expectedSpanKind, expectedAttributes) && AssertSpanDuration(span);
    }

    private bool AssertSpanDuration(Span span)
    {
        var ticks = (long)((span.EndTimeUnixNano - span.StartTimeUnixNano) / 100); // 100ns = 1 tick

        var duration = TimeSpan.FromTicks(ticks);

        return duration > TimeSpan.FromMilliseconds(98); // all async methods have a 100ms delay, need to be a bit lower (due to timer resolution)
    }
}
