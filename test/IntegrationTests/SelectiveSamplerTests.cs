// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
#if NET //for now we ae disabling this on .NET Framework as canary mechanism makes this flaky
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using IntegrationTests.Helpers;

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

        // Test app sleeps for 0.5s, sampling interval set to 0.05s
        Output.WriteLine($"Count: {threadSamples.Count}");
        Assert.InRange(threadSamples.Count, 7, 14);
        Assert.All(threadSamples, sample => Assert.Equal(50u, sample.SelectiveSamplingIntervalMilliseconds));

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

        Assert.All(threadSamples.Where(sample => sample.Source == "selective-sampler"), sample => Assert.Equal(50u, sample.SelectiveSamplingIntervalMilliseconds));

        AssertSnapshotRouting(threadSamples);
    }

    [SkippableFact]
    [Trait("Category", "EndToEnd")]
    public void ExportSelectedSamplesAcrossCpuRuntimeTransitions()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestApplication.SelectiveSampler.Plugins.RuntimeCpuSamplingPlugin, TestApplication.SelectiveSampler, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "TestApplication.SelectiveSampler");

        var (output, _, _) = RunTestApplication(new TestSettings { Arguments = "runtime-cpu-transitions" });
        var samples = ConsoleProfileExporterHelpers.ExtractSamples(output);
        Assert.All(samples.Where(sample => sample.Source == "selective-sampler"), sample => Assert.Equal(50u, sample.SelectiveSamplingIntervalMilliseconds));
        AssertSnapshotRouting(samples);

        var cpuSamples = samples.Where(sample => sample.Source == "continuous-profiler").ToList();
        var firstCpu = cpuSamples.Min(sample => sample.TimestampNanoseconds);
        var lastCpu = cpuSamples.Max(sample => sample.TimestampNanoseconds);
        Assert.Contains(samples, sample => sample.Source == "selective-sampler" && sample.TimestampNanoseconds < firstCpu);
        Assert.Contains(samples, sample => sample.Source == "selective-sampler" && sample.TimestampNanoseconds > lastCpu);
    }

    private static void AssertSnapshotRouting(List<ConsoleThreadSample> samples)
    {
        var selectiveSamples = samples.Where(sample => sample.Source == "selective-sampler").ToList();
        Assert.NotEmpty(selectiveSamples);
        Assert.All(selectiveSamples, sample => Assert.True(HasSpanContextAssociated(sample)));
        var selectiveBatches = selectiveSamples.ToLookup(sample => sample.TimestampNanoseconds);
        var firstSelectiveTimestamp = selectiveSamples.Min(sample => sample.TimestampNanoseconds);
        var lastSelectiveTimestamp = selectiveSamples.Max(sample => sample.TimestampNanoseconds);

        // Ignore startup and shutdown, but keep every CPU capture during the observed selective sampling period.
        var cpuBatches = samples
            .Where(sample => sample.Source == "continuous-profiler")
            .Where(sample => sample.TimestampNanoseconds >= firstSelectiveTimestamp && sample.TimestampNanoseconds <= lastSelectiveTimestamp)
            .GroupBy(sample => sample.TimestampNanoseconds)
            .OrderBy(batch => batch.Key)
            .ToList();
        Assert.True(cpuBatches.Count >= 2, "Expected at least two CPU captures during selective sampling.");

        var missedSelectiveInContinuous = false;
        var matchedCaptures = 0;
        foreach (var batch in cpuBatches)
        {
            // This application selects every active span, so span contexts identify the expected selective subset.
            var selectedSamples = batch.Where(HasSpanContextAssociated).ToList();
            if (selectedSamples.Count > 0)
            {
                AssertExpectedSelectedSamples(selectedSamples);
            }
            else
            {
                Assert.False(missedSelectiveInContinuous, "Missing selective sample in continuous batches allowed only once per test run.");
                missedSelectiveInContinuous = true;
            }

            // Selective backpressure can drop the selective output of a shared capture.
            if (!selectiveBatches.Contains(batch.Key))
            {
                continue;
            }

            matchedCaptures++;
            // Both received outputs must describe the same selected subset, exactly once per capture.
            Assert.Equal(
                selectedSamples.Select(SampleIdentity).OrderBy(identity => identity),
                selectiveBatches[batch.Key].Select(SampleIdentity).OrderBy(identity => identity));
        }

        Assert.True(matchedCaptures >= 2, $"Expected at least two matched CPU and selective captures, got {matchedCaptures}.");

        // CPU backpressure can drop exports while selective sampling continues.
        var cpuTimestamps = cpuBatches.Select(batch => batch.Key).ToHashSet();
        var selectiveOnlyBatches = selectiveBatches
            .Where(batch => batch.Key > cpuBatches[0].Key && batch.Key < cpuBatches[^1].Key && !cpuTimestamps.Contains(batch.Key))
            .ToList();
        Assert.NotEmpty(selectiveOnlyBatches);
        Assert.All(selectiveOnlyBatches, batch => AssertExpectedSelectedSamples([.. batch]));
    }

    private static (long TraceIdHigh, long TraceIdLow, long SpanId, string? ThreadName) SampleIdentity(ConsoleThreadSample sample)
        => (sample.TraceIdHigh, sample.TraceIdLow, sample.SpanId, sample.ThreadName);

    private static void AssertExpectedSelectedSamples(List<ConsoleThreadSample> samples)
    {
        // This test application has one serial flow and a single async handoff. During that handoff,
        // both the source and destination thread can briefly be associated with the same activity.
        Assert.InRange(samples.Count, 1, 2);
        Assert.All(samples, sample => Assert.True(HasSpanContextAssociated(sample)));
        Assert.Single(samples.Select(sample => (sample.TraceIdHigh, sample.TraceIdLow, sample.SpanId)).Distinct());
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
            var sampleSpanId = ActivitySpanId.CreateFromString(spanId.ToString("x16", CultureInfo.InvariantCulture).AsSpan());
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
#endif
