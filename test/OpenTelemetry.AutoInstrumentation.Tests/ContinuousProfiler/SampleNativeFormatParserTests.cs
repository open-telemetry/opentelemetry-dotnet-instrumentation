// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using TestApplication.ContinuousProfiler;

namespace OpenTelemetry.AutoInstrumentation.Tests.ContinuousProfiler;

public class SampleNativeFormatParserTests
{
    [Fact]
    public void BufferedSelectiveBatchesRetainEachSamplesInterval()
    {
        var buffer = new List<byte>();
        byte[] intervals = [40, 80];
        foreach (var interval in intervals)
        {
            // Selective batch headers use version 1 and a big-endian interval.
            buffer.AddRange([0x0A, 0, 0, 0, 1, 0, 0, 0, interval, 0x09]);
            buffer.AddRange(new byte[8]); // Selective sample timestamp.
            buffer.AddRange(new byte[26]); // Empty thread name and span context.

            // Define frame code 1 as "f", terminate the stack and end the batch.
            buffer.AddRange([0xFF, 0xFF, 0, 1, (byte)'f', 0, 0, 0, 0x0B]);
        }

        var parser = new SampleNativeFormatParser();
        var bytes = buffer.ToArray();
        var samples = parser.ParseSelectiveSamplerSamples(bytes, bytes.Length);

        Assert.Equal(intervals.Select(interval => (uint)interval), samples.Select(sample => sample.SelectiveSamplingIntervalMilliseconds));
        Assert.All(samples, sample => Assert.Equal("f", Assert.Single(sample.Frames)));
    }

    [Fact]
    public void CpuBatchesContainNoSelectiveSamplingFields()
    {
        var buffer = new List<byte>();
        byte[] timestamps = [40, 80];
        foreach (var timestamp in timestamps)
        {
            // CPU batch headers contain only version 2 and a big-endian 8-byte timestamp.
            buffer.AddRange([0x01, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, timestamp]);
            buffer.Add(0x02);
            buffer.AddRange(new byte[26]); // Empty thread name and span context, followed immediately by the stack.
            buffer.AddRange([0xFF, 0xFF, 0, 1, (byte)'f', 0, 0, 0]);
            buffer.Add(0x02);
            buffer.AddRange(new byte[26]);
            buffer.AddRange([0, 1, 0, 0, 0x06]); // Reuse frame code 1, terminate the stack and end the batch.
        }

        var parser = new SampleNativeFormatParser();
        var bytes = buffer.ToArray();
        var samples = parser.ParseThreadSamples(bytes, bytes.Length);

        Assert.NotNull(samples);
        Assert.Equal(timestamps.SelectMany(timestamp => Enumerable.Repeat(timestamp * 1_000_000L, 2)), samples.Select(sample => sample.TimestampNanoseconds));
        Assert.All(samples, sample =>
        {
            Assert.Equal("continuous-profiler", sample.Source);
            Assert.Equal(0u, sample.SelectiveSamplingIntervalMilliseconds);
            Assert.Equal("f", Assert.Single(sample.Frames));
        });
    }

    [Theory]
    [InlineData((byte)1)]
    [InlineData((byte)3)]
    public void CpuBatchesWithUnsupportedVersionsAreRejected(byte version)
    {
        byte[] buffer = [0x01, 0, 0, 0, version, 0, 0, 0, 0, 0, 0, 0, 40, 0x06];
        var parser = new SampleNativeFormatParser();

        Assert.Null(parser.ParseThreadSamples(buffer, buffer.Length));
    }
}
