// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal class BufferProcessor
{
    // If you change any of these constants, check with continuous_profiler.cpp first
    private const int BufferSize = 200 * 1024;

    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    private readonly byte[] _buffer = new byte[BufferSize];

    private readonly Dictionary<SampleType, (Action<byte[], int, uint, CancellationToken> Handler, TimeSpan ExportTimeout)> _sampleHandlers;
    private readonly Func<SampleType, byte[], (int Read, uint SamplingInterval)> _readBuffer;

    public BufferProcessor(Dictionary<SampleType, (Action<byte[], int, uint, CancellationToken> Handler, TimeSpan ExportTimeout)> sampleHandlers)
        : this(sampleHandlers, ReadBuffer)
    {
    }

    internal BufferProcessor(
        Dictionary<SampleType, (Action<byte[], int, uint, CancellationToken> Handler, TimeSpan ExportTimeout)> sampleHandlers,
        Func<SampleType, byte[], (int Read, uint SamplingInterval)> readBuffer)
    {
        _sampleHandlers = sampleHandlers ?? throw new ArgumentNullException(nameof(sampleHandlers));
        _readBuffer = readBuffer ?? throw new ArgumentNullException(nameof(readBuffer));
    }

    public void Process()
    {
        foreach (var sampleType in _sampleHandlers.Keys)
        {
            try
            {
                var (read, samplingInterval) = _readBuffer(sampleType, _buffer);
                if (read <= 0)
                {
                    continue;
                }

                var (handler, timeout) = _sampleHandlers[sampleType];

                using var cts = new CancellationTokenSource(timeout);
                handler(_buffer, read, samplingInterval, cts.Token);
            }
            catch (Exception e)
            {
                Logger.Warning(e, $"Failed to process {sampleType} samples.");
            }
        }
    }

    private static (int Read, uint SamplingInterval) ReadBuffer(SampleType sampleType, byte[] buffer)
    {
        uint samplingInterval = 0;
        var read = sampleType switch
        {
            SampleType.Continuous => NativeMethods.ContinuousProfilerReadThreadSamples(buffer.Length, buffer, out samplingInterval),
            SampleType.SelectedThreads => NativeMethods.SelectiveSamplerReadThreadSamples(buffer.Length, buffer),
#if NET
            SampleType.Allocation => NativeMethods.ContinuousProfilerReadAllocationSamples(buffer.Length, buffer),
#else
            SampleType.Allocation => 0,
#endif
            _ => throw new ArgumentOutOfRangeException(nameof(sampleType), sampleType, null)
        };

        return (read, samplingInterval);
    }
}
