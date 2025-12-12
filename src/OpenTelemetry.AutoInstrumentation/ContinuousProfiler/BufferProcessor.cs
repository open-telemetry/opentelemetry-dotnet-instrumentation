// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal class BufferProcessor
{
    // If you change any of these constants, check with continuous_profiler.cpp first
    private const int BufferSize = 200 * 1024;

    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    private readonly byte[] _buffer = new byte[BufferSize];

    private readonly Dictionary<SampleType, (Action<byte[], int, CancellationToken> Handler, TimeSpan ExportTimeout)> _sampleHandlers;

    public BufferProcessor(Dictionary<SampleType, (Action<byte[], int, CancellationToken> Handler, TimeSpan ExportTimeout)> sampleHandlers)
    {
        _sampleHandlers = sampleHandlers ?? throw new ArgumentNullException(nameof(sampleHandlers));
    }

    public void Process()
    {
        foreach (var sampleType in _sampleHandlers.Keys)
        {
            var read = ReadBuffer(sampleType);
            if (read <= 0)
            {
                continue;
            }

            var (handler, timeout) = _sampleHandlers[sampleType];

            using var cts = new CancellationTokenSource(timeout);
            try
            {
                handler(_buffer, read, cts.Token);
            }
            catch (Exception e)
            {
                Logger.Warning(e, $"Failed to process {sampleType} samples.");
            }
        }
    }

    private int ReadBuffer(SampleType sampleType)
    {
        return sampleType switch
        {
            SampleType.Continuous => NativeMethods.ContinuousProfilerReadThreadSamples(_buffer.Length, _buffer),
            SampleType.SelectedThreads => NativeMethods.SelectiveSamplerReadThreadSamples(_buffer.Length, _buffer),
            SampleType.Allocation => NativeMethods.ContinuousProfilerReadAllocationSamples(_buffer.Length, _buffer),
            _ => throw new ArgumentOutOfRangeException(nameof(sampleType), sampleType, null)
        };
    }
}

#endif
