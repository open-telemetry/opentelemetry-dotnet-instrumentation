// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text;

namespace TestApplication.ContinuousProfiler;

/// <summary>
/// Parser the native code's pause-time-optimized format.
/// </summary>
public class SampleNativeFormatParser
{
    // TODO use value from ContinuousProfilerProcessor.BackgroundThreadName when it will be moved to main project
    public const string BackgroundThreadName = "OpenTelemetry Continuous Profiler Thread";
    private static readonly UnicodeEncoding UnicodeEncoding = new();
    private readonly bool _frequentSamplingEnabled;

    public SampleNativeFormatParser(bool frequentSamplingEnabled = false)
    {
        _frequentSamplingEnabled = frequentSamplingEnabled;
    }

    /// <summary>
    /// Parses the thread sample batch.
    /// </summary>
    /// <param name="buffer">byte array containing native thread samples format data</param>
    /// <param name="read">how much of the buffer is actually used</param>
    internal List<ThreadSample>? ParseThreadSamples(byte[] buffer, int read)
    {
        uint batchThreadIndex = 0;
        var samples = new List<ThreadSample>();
        long sampleStartMillis = 0;

        var position = 0;

        // common for samples in a batch
        var codeDictionary = new Dictionary<int, string>();

        try
        {
            while (position < read)
            {
                var operationCode = buffer[position];
                position++;
                if (operationCode == OpCodes.StartBatch)
                {
                    var version = ReadInt(buffer, ref position);
                    if (version != 1)
                    {
                        return null; // not able to parse
                    }

                    sampleStartMillis = ReadInt64(buffer, ref position);

                    // TODO Debug logs to measure overhead https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/3205
                    /* if (IsLogLevelDebugEnabled)
                    {
                        var sampleStart = new DateTime(
                            (sampleStartMillis * TimeSpan.TicksPerMillisecond) + TimeConstants.UnixEpochInTicks).ToLocalTime();
                        Log.Debug(
                            "Parsing thread samples captured at {date} {time}",
                            sampleStart.ToLongDateString(),
                            sampleStart.ToLongTimeString());
                    }*/
                }
                else if (operationCode == OpCodes.StartSample)
                {
                    var threadName = ReadString(buffer, ref position);
                    var traceIdHigh = ReadInt64(buffer, ref position);
                    var traceIdLow = ReadInt64(buffer, ref position);
                    var spanId = ReadInt64(buffer, ref position);

                    var selectedForFrequentSampling = false;

                    if (_frequentSamplingEnabled)
                    {
                        selectedForFrequentSampling = buffer[position++] == 1;
                    }

                    var threadIndex = batchThreadIndex++;

                    var code = ReadShort(buffer, ref position);
                    if (code == 0)
                    {
                        // Empty stack, skip this sample.
                        continue;
                    }

                    // TODO: revisit
                    var threadSample = new ThreadSample(
                        sampleStartMillis,
                        traceIdHigh,
                        traceIdLow,
                        spanId,
                        threadName,
                        "continuous-profiler",
                        threadIndex,
                        selectedForFrequentSampling);

                    ReadStackFrames(code, threadSample, codeDictionary, buffer, ref position);

                    if (threadName == BackgroundThreadName)
                    {
                        // TODO add configuration option to include the sampler thread. By default remove it.
                        continue;
                    }

                    samples.Add(threadSample);
                }
                else if (operationCode == OpCodes.EndBatch)
                {
                    // end batch, nothing here
                }
                else if (operationCode == OpCodes.BatchStats)
                {
                    var microsSuspended = ReadInt(buffer, ref position);
                    var numThreads = ReadInt(buffer, ref position);
                    var totalFrames = ReadInt(buffer, ref position);
                    var numCacheMisses = ReadInt(buffer, ref position);

                    // TODO Debug logs to measure overhead https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/3205
                    /*if (IsLogLevelDebugEnabled)
                    {
                        Log.Debug(
                            "CLR was suspended for {microsSuspended} microseconds to collect a thread sample batch: threads={numThreads} frames={totalFrames} misses={numCacheMisses}",
                            new object[] { microsSuspended, numThreads, totalFrames, numCacheMisses });
                    }*/
                }
                else
                {
                    position = read + 1;

                    // TODO Debug log to handle unexpected buffer https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/3205
                    /* if (IsLogLevelDebugEnabled)
                    {
                        Log.Debug("Not expected operation code while parsing thread stack trace: '{0}'. Operation will be ignored.", operationCode);
                    } */
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e + "Unexpected error while parsing thread samples.");
        }

        return samples;
    }

    /// <summary>
    /// Parses the allocation sample batch.
    /// </summary>
    /// <param name="buffer">byte array containing native allocation samples format data</param>
    /// <param name="read">how much of the buffer is actually used</param>
    internal List<AllocationSample> ParseAllocationSamples(byte[] buffer, int read)
    {
        var allocationSamples = new List<AllocationSample>();
        var position = 0;

        try
        {
            while (position < read)
            {
                var operationCode = buffer[position++];

                if (operationCode == OpCodes.AllocationSample)
                {
                    var timestampMillis = ReadInt64(buffer, ref position);
                    var allocatedSize = ReadInt64(buffer, ref position); // Technically uint64 but whatever
                    var typeName = ReadString(buffer, ref position);
                    var threadName = ReadString(buffer, ref position);
                    var traceIdHigh = ReadInt64(buffer, ref position);
                    var traceIdLow = ReadInt64(buffer, ref position);
                    var spanId = ReadInt64(buffer, ref position);

                    var threadSample = new ThreadSample(
                        timestampMillis,
                        traceIdHigh,
                        traceIdLow,
                        spanId,
                        threadName,
                        "allocation");

                    var code = ReadShort(buffer, ref position);

                    // each allocation sample has independently coded strings
                    var codeDictionary = new Dictionary<int, string>();

                    ReadStackFrames(code, threadSample, codeDictionary, buffer, ref position);
                    if (threadName == BackgroundThreadName)
                    {
                        // TODO: add configuration option to include the sampler thread. By default remove it.
                        continue;
                    }

                    allocationSamples.Add(new AllocationSample(allocatedSize, typeName, threadSample));
                }
                else
                {
                    position = read + 1;

                    /* if (IsLogLevelDebugEnabled)
                    {
                        Log.Debug("Not expected operation code while parsing allocation sample: '{0}'. Operation will be ignored.", operationCode);
                    } */
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e + "Unexpected error while parsing allocation samples.");
        }

        return allocationSamples;
    }

    internal List<ThreadSample> ParseSelectiveSamplerSamples(byte[] buffer, int read)
    {
        var selectiveSamplerSamples = new List<ThreadSample>();
        var position = 0;

        uint threadIndex = 0;

        var codeDictionary = new Dictionary<int, string>();

        try
        {
            while (position < read)
            {
                var operationCode = buffer[position++];

                if (operationCode == OpCodes.SelectiveSampleBatchStart)
                {
                    // each batch has independently coded strings
                    codeDictionary.Clear();
                }
                else if (operationCode == OpCodes.SelectiveSample)
                {
                    var timestampMillis = ReadInt64(buffer, ref position);
                    var threadName = ReadString(buffer, ref position);
                    var traceIdHigh = ReadInt64(buffer, ref position);
                    var traceIdLow = ReadInt64(buffer, ref position);
                    var spanId = ReadInt64(buffer, ref position);

                    var threadSample = new ThreadSample(
                        timestampMillis,
                        traceIdHigh,
                        traceIdLow,
                        spanId,
                        threadName,
                        "selective-sampler",
                        threadIndex++,
                        true);

                    var code = ReadShort(buffer, ref position);

                    ReadStackFrames(code, threadSample, codeDictionary, buffer, ref position);
                    selectiveSamplerSamples.Add(threadSample);
                }
                else if (operationCode == OpCodes.SelectiveSampleBatchEnd)
                {
                }
                else
                {
                    position = read + 1;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e + "Unexpected error while parsing selected threads samples.");
        }

        return selectiveSamplerSamples;
    }

    private static string ReadString(byte[] buffer, ref int position)
    {
        var length = ReadShort(buffer, ref position);
        var s = UnicodeEncoding.GetString(buffer, position, length * 2);
        position += 2 * length;
        return s;
    }

    private static short ReadShort(byte[] buffer, ref int position)
    {
        var s1 = (short)(buffer[position] & 0xFF);
        s1 <<= 8;
        var s2 = (short)(buffer[position + 1] & 0xFF);
        position += 2;
        return (short)(s1 + s2);
    }

    private static int ReadInt(byte[] buffer, ref int position)
    {
        var i1 = buffer[position] & 0xFF;
        i1 <<= 24;
        var i2 = buffer[position + 1] & 0xFF;
        i2 <<= 16;
        var i3 = buffer[position + 2] & 0xFF;
        i3 <<= 8;
        var i4 = buffer[position + 3] & 0xFF;
        position += 4;
        return i1 + i2 + i3 + i4;
    }

    private static long ReadInt64(byte[] buffer, ref int position)
    {
        long l1 = buffer[position] & 0xFF;
        l1 <<= 56;
        long l2 = buffer[position + 1] & 0xFF;
        l2 <<= 48;
        long l3 = buffer[position + 2] & 0xFF;
        l3 <<= 40;
        long l4 = buffer[position + 3] & 0xFF;
        l4 <<= 32;
        long l5 = buffer[position + 4] & 0xFF;
        l5 <<= 24;
        long l6 = buffer[position + 5] & 0xFF;
        l6 <<= 16;
        long l7 = buffer[position + 6] & 0xFF;
        l7 <<= 8;
        long l8 = buffer[position + 7] & 0xFF;
        position += 8;
        return l1 + l2 + l3 + l4 + l5 + l6 + l7 + l8;
    }

    /// <summary>
    /// Reads stack frames until 0 (no more frames) is encountered
    /// </summary>
    private static void ReadStackFrames(short code, ThreadSample threadSample, Dictionary<int, string> dictionary, byte[] buffer, ref int position)
    {
        while (code != 0)
        {
            string? value;
            if (code < 0)
            {
                value = ReadString(buffer, ref position);

                dictionary[-code] = value;
            }
            else
            {
                value = dictionary[code];
            }

            if (value != null)
            {
                threadSample.Frames.Add(value);
            }

            code = ReadShort(buffer, ref position);
        }
    }

    private static class OpCodes
    {
        /// <summary>
        /// Marks the start of a batch of thread samples, see THREAD_SAMPLES_START_BATCH on native code.
        /// </summary>
        public const byte StartBatch = 0x01;

        /// <summary>
        /// Marks the start of a thread sample, see THREAD_SAMPLES_START_SAMPLE on native code.
        /// </summary>
        public const byte StartSample = 0x02;

        /// <summary>
        /// Marks the end of a batch of thread samples, see THREAD_SAMPLES_END_BATCH on native code.
        /// </summary>
        public const byte EndBatch = 0x06;

        /// <summary>
        /// Marks the beginning of a section with statistics, see THREAD_SAMPLES_FINAL_STATS on native code.
        /// </summary>
        public const byte BatchStats = 0x07;

        /// <summary>
        /// Marks the start of an allocation sample, see THREAD_SAMPLES_ALLOCATION_SAMPLE on native code.
        /// </summary>
        public const byte AllocationSample = 0x08;

        /// <summary>
        /// Marks the start of a selective thread sample, see kSelectiveSample on native code.
        /// </summary>
        public const byte SelectiveSample = 0x09;

        /// <summary>
        /// Marks the start of a selective thread samples batch, see kSelectedThreadsStartBatch on native code.
        /// </summary>
        public const byte SelectiveSampleBatchStart = 0x0A;

        /// <summary>
        /// Marks the end of a selective thread samples batch, see kSelectedThreadsEndBatch on native code.
        /// </summary>
        public const byte SelectiveSampleBatchEnd = 0x0B;
    }
}
