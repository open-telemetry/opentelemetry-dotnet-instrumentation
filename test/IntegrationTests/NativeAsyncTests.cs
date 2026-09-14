// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Trace.V1;

namespace IntegrationTests;

public class NativeAsyncTests : TestHelper
{
    private static readonly TimeSpan MinimumAsyncDuration = TimeSpan.FromMilliseconds(84);

    public NativeAsyncTests(ITestOutputHelper output)
        : base("NativeAsync", output)
    {
#if NET10_0
        SetEnvironmentVariable("DOTNET_RuntimeAsync", "1");
#endif
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void InstrumentsRuntimeAndConventionalAsyncMethods()
    {
        EnableBytecodeInstrumentation();
        EnableFileBasedConfig();
        using var collector = new MockSpansCollector(Output);
        SetFileBasedExporter(collector);

        string[] successfulSpans =
        [
            "RuntimeAsync-Task",
            "RuntimeAsync-TaskOfT",
            "RuntimeAsync-ValueTask",
            "RuntimeAsync-ValueTaskOfT",
            "RuntimeAsync-MultidimensionalArray",
            "RuntimeAsync-GenericTask",
            "ConventionalAsync-Task",
            "ConventionalAsync-TaskOfT",
            "ConventionalAsync-ValueTask",
            "ConventionalAsync-ValueTaskOfT",
        ];

        string[] faultedSpans =
        [
            "RuntimeAsync-FaultedTask",
            "RuntimeAsync-FaultedTaskOfT",
            "RuntimeAsync-FaultedValueTask",
            "RuntimeAsync-FaultedValueTaskOfT",
            "ConventionalAsync-FaultedTask",
            "ConventionalAsync-FaultedValueTaskOfT",
        ];

        foreach (var spanName in successfulSpans)
        {
            ExpectSpan(collector, spanName, isError: false);
        }

        foreach (var spanName in faultedSpans)
        {
            ExpectSpan(collector, spanName, isError: true);
        }

        RunTestApplication();

        collector.AssertExpectations();
        collector.AssertEmpty();
    }

    private static void ExpectSpan(MockSpansCollector collector, string spanName, bool isError)
    {
        collector.Expect(
            "OpenTelemetry.AutoInstrumentation.NoCode",
            VersionHelper.AutoInstrumentationVersion,
            span => IsExpectedSpan(span, spanName, isError),
            $"Span name: '{spanName}', error: '{isError}', duration greater than '{MinimumAsyncDuration}'");
    }

    private static bool IsExpectedSpan(Span span, string spanName, bool isError)
    {
        var ticks = (long)((span.EndTimeUnixNano - span.StartTimeUnixNano) / 100);
        var hasExpectedDuration = TimeSpan.FromTicks(ticks) > MinimumAsyncDuration;
        var hasExpectedStatus = isError
            ? span.Status?.Code == Status.Types.StatusCode.Error
            : span.Status?.Code != Status.Types.StatusCode.Error;

        return span.Name == spanName && hasExpectedDuration && hasExpectedStatus;
    }
}
#endif
