// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class ContinuousProfilerSpanStoppageHandlingTests : TestHelper
{
    public ContinuousProfilerSpanStoppageHandlingTests(ITestOutputHelper testOutputHelper)
        : base("ProfilerSpanStoppageHandling", testOutputHelper)
    {
    }

    [Fact]
    public void WhenSpanIsStopped_ThreadContextIsClearedForAllThreadsWithThatSpan()
    {
        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestApplication.SelectiveSampler.Plugins.MixedModeSamplingPlugin, TestApplication.ProfilerSpanStoppageHandling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

        var (output, _, processId) = RunTestApplication();

        var threadSamples = ConsoleProfileExporterHelpers.ExtractSamples(output);

        var groupedByTimestampAscending = threadSamples.GroupBy(sample => sample.TimestampNanoseconds).OrderBy(samples => samples.Key);

        // The last batch is expected to be collected after request already completed, and it's activity was stopped.
        Assert.DoesNotContain(groupedByTimestampAscending.Last(), sample => sample.SpanId != 0);
    }
}
