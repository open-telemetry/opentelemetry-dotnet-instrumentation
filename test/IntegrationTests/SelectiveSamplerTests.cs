// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Runtime.InteropServices;
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class SelectiveSamplerTests : TestHelper
{
    public SelectiveSamplerTests(ITestOutputHelper output)
        : base("SelectiveSampler", output)
    {
    }

    [SkippableFact]
    [Trait("Category", "EndToEnd")]
    public void ExportThreadSamples()
    {
        // TODO: Huge variance in delay between samples on MacOS on CI
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));

        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestApplication.SelectiveSampler.Plugins.SelectiveSamplerPlugin, TestApplication.SelectiveSampler, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "TestApplication.SelectiveSampler");

        collector.Expect("TestApplication.SelectiveSampler", span => span.Name == "outer", "Outer span from test application.");
        collector.Expect("TestApplication.SelectiveSampler", span => span.Name == "inner", "Inner span from test application.");

        var (output, _, processId) = RunTestApplication();

        var threadSamples = ConsoleProfileExporterHelpers.ExtractSamples(output);

        var currentStartTime = ToDateTime(threadSamples[0].TimestampNanoseconds);

        foreach (var sample in threadSamples.Skip(1))
        {
            var nextStartTime = ToDateTime(sample.TimestampNanoseconds);
            var diff = (nextStartTime - currentStartTime).TotalMilliseconds;
            Output.WriteLine($"Time diff between consecutive samples: {diff}");
            Assert.InRange(diff, 50, 70);
            currentStartTime = nextStartTime;
        }

        // Test app sleeps for 0.5s, sampling interval set to 0.05s
        Output.WriteLine($"Count: {threadSamples.Count}");
        Assert.InRange(threadSamples.Count, 7, 14);

        var threadNames = threadSamples.Select(sample => sample.ThreadName).Distinct(StringComparer.InvariantCultureIgnoreCase);

        var threadSampleSpanContexts = threadSamples
            .Select(ts => ((ulong)ts.SpanId, (ulong)ts.TraceIdHigh, (ulong)ts.TraceIdLow))
            .Distinct();

        Assert.Equal(2, threadSampleSpanContexts.Count());
        Assert.Equal(2, threadNames.Count());

        collector.ExpectCollected(collectedSpans => VerifyMatching(threadSampleSpanContexts, collectedSpans));
        collector.AssertExpectations();
    }

    [SkippableFact]
    [Trait("Category", "EndToEnd")]
    public void ExportThreadSamplesInMixedMode()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestApplication.SelectiveSampler.Plugins.MixedModeSamplingPlugin, TestApplication.SelectiveSampler, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "TestApplication.SelectiveSampler");

        var (output, _, processId) = RunTestApplication();

        var threadSamples = ConsoleProfileExporterHelpers.ExtractSamples(output);

        var groupedByTimestampAscending = threadSamples.GroupBy(sample => sample.TimestampNanoseconds).OrderBy(samples => samples.Key);

        // Based on the test app, samples for all the threads should be collected at least 2 times.
        Assert.True(groupedByTimestampAscending.Count(
            samples =>
                !IndicatesSelectiveSampling(samples) &&
                samples.Any(HasSpanContextAssociated) &&
                samples.Count(sample => sample.SelectedForFrequentSampling) == 1)
                    > 1);

        var counter = 0;

        // Sampling starts early, at the start of instrumentation init.
        var groupingStartingWithAllThreadSamples = groupedByTimestampAscending.SkipWhile(
            samples =>
                IndicatesSelectiveSampling(samples) ||
                CollectedBeforeSpanStarted(samples) ||
                CollectedBeforeFrequentSamplingStarted(samples));

        foreach (var group in groupingStartingWithAllThreadSamples)
        {
            // Based on plugin configuration, the expectation is for every 4th
            // batch to contain multiple samples as a result of continuous profiling.
            if (counter % 4 == 0)
            {
#if NET
                // on .NET Framework there is no guarantee that samples collected from all threads
                Assert.NotEqual(1, group.Count());
#endif
                // Sample for thread selected for frequent sampling when collecting samples of all threads
                // should be marked with SelectedForFrequentSampling flag.
                Assert.Single(group, sample => sample.SelectedForFrequentSampling);
            }
            else
            {
                Assert.Single(group);
            }

            Assert.Single(group, HasSpanContextAssociated);

            counter++;
        }
    }

    private static bool IndicatesSelectiveSampling(IGrouping<long, ConsoleThreadSample> samples)
    {
        return samples.Count() == 1 && samples.Single().Source == "selective-sampler";
    }

    private static bool CollectedBeforeSpanStarted(IGrouping<long, ConsoleThreadSample> samples)
    {
        return !samples.Any(HasSpanContextAssociated);
    }

    private static bool CollectedBeforeFrequentSamplingStarted(IGrouping<long, ConsoleThreadSample> samples)
    {
        return !samples.Any(sample => sample.SelectedForFrequentSampling);
    }

    private static bool HasSpanContextAssociated(ConsoleThreadSample sample)
    {
        return sample.TraceIdHigh != 0 && sample.TraceIdLow != 0 && sample.SpanId != 0;
    }

    private static DateTime ToDateTime(long timestampNanoseconds)
    {
        const int nanosecondsInTick = 100;

#if NET
        return DateTime.UnixEpoch.AddTicks(timestampNanoseconds / nanosecondsInTick);
#else
        const int daysTo1970 = 719_162;
        const long unixEpochTicks = daysTo1970 * TimeSpan.TicksPerDay;

        return new DateTime(unixEpochTicks, DateTimeKind.Utc).AddTicks(timestampNanoseconds / nanosecondsInTick);
#endif
    }

    private static bool VerifyMatching(
        IEnumerable<(ulong SpanId, ulong TraceIdHigh, ulong TraceIdLow)> threadSampleSpanContexts,
        ICollection<MockSpansCollector.Collected> collectedSpans)
    {
        foreach (var (spanId, traceIdHigh, traceIdLow) in threadSampleSpanContexts)
        {
            // Reverse the conversion done in TryParseTraceContext methods in NativeMethods.cs
            var sampleSpanId = ActivitySpanId.CreateFromString(spanId.ToString("x16").AsSpan());
            var sampleTraceId = ActivityTraceId.CreateFromString($"{traceIdHigh:x16}{traceIdLow:x16}".AsSpan());

            var match = collectedSpans.Any(c =>
            {
                var spanSpanId = ActivitySpanId.CreateFromBytes(c.Span.SpanId.ToByteArray());
                var spanTraceId = ActivityTraceId.CreateFromBytes(c.Span.TraceId.ToByteArray());
                return spanSpanId == sampleSpanId && spanTraceId == sampleTraceId;
            });

            if (match)
            {
                continue;
            }

            return false;
        }

        return true;
    }
}
