// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
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
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
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

        RunTestApplication();

        collector.AssertExpectations();
    }
}
