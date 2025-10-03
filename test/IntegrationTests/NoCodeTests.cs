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
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethodStatic");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethod0");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethodA");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethod1String");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethod1Int");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethod2");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethod3");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethod4");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethod5");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethod6");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethod7");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethod8");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethod9");

        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-ReturningTestMethodStatic");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-ReturningTestMethod0");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-ReturningStringTestMethod");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-ReturningCustomClassTestMethod");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-ReturningTestMethod1String");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-ReturningTestMethod1Int");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-ReturningTestMethod2");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-ReturningTestMethod3");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-ReturningTestMethod4");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-ReturningTestMethod5");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-ReturningTestMethod6");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-ReturningTestMethod7");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-ReturningTestMethod8");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-ReturningTestMethod9");

        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethodStaticAsync" && AssertSpanDuration(x));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethod0Async" && AssertSpanDuration(x));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethodAAsync" && AssertSpanDuration(x));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethod1StringAsync" && AssertSpanDuration(x));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethod1IntAsync" && AssertSpanDuration(x));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethod2Async" && AssertSpanDuration(x));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethod3Async" && AssertSpanDuration(x));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethod4Async" && AssertSpanDuration(x));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethod5Async" && AssertSpanDuration(x));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethod6Async" && AssertSpanDuration(x));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethod7Async" && AssertSpanDuration(x));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethod8Async" && AssertSpanDuration(x));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-TestMethod9Async" && AssertSpanDuration(x));

        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-IntTaskTestMethodAsync" && AssertSpanDuration(x));
#if NET
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-ValueTaskTestMethodAsync" && AssertSpanDuration(x));
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-IntValueTaskTestMethodAsync" && AssertSpanDuration(x));
#endif

        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-GenericTestMethod");
        collector.Expect("OpenTelemetry.AutoInstrumentation.NoCode", x => x.Name == "Span-GenericTestMethodAsync" && AssertSpanDuration(x));

        RunTestApplication();

        collector.AssertExpectations();
    }

    private bool AssertSpanDuration(Span span)
    {
        var ticks = (long)((span.EndTimeUnixNano - span.StartTimeUnixNano) / 100); // 100ns = 1 tick

        var duration = TimeSpan.FromTicks(ticks);

        return duration > TimeSpan.FromMilliseconds(98); // all async methods have a 100ms delay, need to be a bit lower (due to timer resolution)
    }
}
