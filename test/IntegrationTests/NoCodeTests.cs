// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
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
        SetExporter(collector);
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertSpan(x, "Span-TestMethodStatic"));
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

        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => AssertAsyncSpan(x, "Span-TestMethodStaticAsync"));
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

    private bool AssertSpan(Span span, string expectedSpanName, Span.Types.SpanKind expectedSpanKind = Span.Types.SpanKind.Internal)
    {
        return span.Name == expectedSpanName && span.Kind == expectedSpanKind;
    }

    private bool AssertAsyncSpan(Span span, string expectedSpanName, Span.Types.SpanKind expectedSpanKind = Span.Types.SpanKind.Internal)
    {
        return AssertSpan(span, expectedSpanName, expectedSpanKind) && AssertSpanDuration(span);
    }

    private bool AssertSpanDuration(Span span)
    {
        var ticks = (long)((span.EndTimeUnixNano - span.StartTimeUnixNano) / 100); // 100ns = 1 tick

        var duration = TimeSpan.FromTicks(ticks);

        return duration > TimeSpan.FromMilliseconds(98); // all async methods have a 100ms delay, need to be a bit lower (due to timer resolution)
    }
}
