// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using System.Text.Json;
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class SelectiveSamplerTests : TestHelper
{
    public SelectiveSamplerTests(ITestOutputHelper output)
        : base("SelectiveSampler", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void ExportThreadSamples()
    {
        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestApplication.SelectiveSampler.Plugins.SelectiveSamplerPlugin, TestApplication.SelectiveSampler, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "TestApplication.SelectiveSampler");

        var (output, _, processId) = RunTestApplication();

        var batchSeparator = $"{Environment.NewLine}{Environment.NewLine}";
        var lines = output.Split(batchSeparator);
        var deserializedSampleBatches = lines[..^1].Select(sample => JsonSerializer.Deserialize<List<ThreadSample>>(sample)).ToList();

        var threadSamples = new List<ThreadSample>();
        foreach (var batch in deserializedSampleBatches)
        {
            threadSamples.AddRange(batch!);
        }

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
        Assert.InRange(threadSamples.Count, 7, 10);
        // TODO: verify expectation: first few stack samples should come from the Main thread,
        // rest of them - from a thread pool thread.
    }

    private static DateTime ToDateTime(long timestampNanoseconds)
    {
        const int nanosecondsInTick = 100;
        return DateTime.UnixEpoch.AddTicks(timestampNanoseconds / nanosecondsInTick);
    }

    private class ThreadSample
    {
        public long TimestampNanoseconds { get; set; }

        public long SpanId { get; set; }

        public long TraceIdHigh { get; set; }

        public long TraceIdLow { get; set; }

        public string? ThreadName { get; set; }

        public uint ThreadIndex { get; set; }

        public IList<string> Frames { get; set; } = new List<string>();
    }
}
#endif

