// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using TestApplication.ContinuousProfiler;

namespace OpenTelemetry.AutoInstrumentation.Tests.ContinuousProfiler;

public class SampleNativeFormatParserTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void BufferedBatchesRetainEachSamplesSelectiveInterval(bool cpu, bool frequentSamplingEnabled)
    {
        var buffer = new List<byte>();
        byte[] intervals = cpu ? [40, 80, 0] : [40, 80];
        foreach (var interval in intervals)
        {
            // Golden version 2 headers use big-endian integers and an 8-byte CPU timestamp.
            buffer.AddRange(cpu ? [0x01, 0, 0, 0, 2] : [0x0A, 0, 0, 0, 2]);
            if (cpu)
            {
                buffer.AddRange(new byte[8]);
            }

            buffer.AddRange([0, 0, 0, interval]);
            buffer.Add(cpu ? (byte)0x02 : (byte)0x09);
            if (!cpu)
            {
                buffer.AddRange(new byte[8]); // Selective sample timestamp.
            }

            buffer.AddRange(new byte[26]); // Empty thread name and span context.
            if (cpu && interval != 0)
            {
                buffer.Add(1); // SelectedForFrequentSampling is absent when selective sampling is disabled.
            }

            // Define frame code 1 as "f", terminate the stack and end the batch.
            buffer.AddRange([0xFF, 0xFF, 0, 1, (byte)'f', 0, 0, 0]);
            buffer.Add(cpu ? (byte)0x06 : (byte)0x0B);
        }

        var parser = new SampleNativeFormatParser(frequentSamplingEnabled);
        var bytes = buffer.ToArray();
        var samples = cpu
            ? parser.ParseThreadSamples(bytes, bytes.Length)
            : parser.ParseSelectiveSamplerSamples(bytes, bytes.Length);

        Assert.NotNull(samples);
        Assert.Equal(intervals.Select(interval => (uint)interval), samples.Select(sample => sample.SelectiveSamplingIntervalMilliseconds));
        Assert.Equal(intervals.Select(interval => interval != 0), samples.Select(sample => sample.SelectedForFrequentSampling));
        Assert.All(samples, sample => Assert.Equal("f", Assert.Single(sample.Frames)));
    }
}
