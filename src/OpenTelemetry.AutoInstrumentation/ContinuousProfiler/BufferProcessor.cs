// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER

using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal class BufferProcessor
{
    // If you change any of these constants, check with continuous_profiler.cpp first
    private const int BufferSize = 200 * 1024;

    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    private readonly bool _threadSamplingEnabled;
    private readonly bool _allocationSamplingEnabled;
    private readonly Action<byte[], int, CancellationToken> _exportThreadSamplesMethod;
    private readonly Action<byte[], int, CancellationToken> _exportAllocationSamplesMethod;
    private readonly byte[] _buffer = new byte[BufferSize];

    public BufferProcessor(bool threadSamplingEnabled, bool allocationSamplingEnabled, Action<byte[], int, CancellationToken> threadSamplesMethod, Action<byte[], int, CancellationToken> allocationSamplesMethod)
    {
        _threadSamplingEnabled = threadSamplingEnabled;
        _allocationSamplingEnabled = allocationSamplingEnabled;
        _exportThreadSamplesMethod = threadSamplesMethod;
        _exportAllocationSamplesMethod = allocationSamplesMethod;
    }

    public void Process(CancellationToken cancellationToken)
    {
        if (_threadSamplingEnabled)
        {
            ProcessThreadSamples(cancellationToken);
        }

        if (_allocationSamplingEnabled)
        {
            ProcessAllocationSamples(cancellationToken);
        }
    }

    private void ProcessThreadSamples(CancellationToken cancellationToken)
    {
        try
        {
            var read = NativeMethods.ContinuousProfilerReadThreadSamples(_buffer.Length, _buffer);
            if (read <= 0)
            {
                return;
            }

            _exportThreadSamplesMethod(_buffer, read, cancellationToken);
        }
        catch (Exception e)
        {
            Logger.Warning(e, "Failed to process thread samples.");
        }
    }

    private void ProcessAllocationSamples(CancellationToken cancellationToken)
    {
        try
        {
            var read = NativeMethods.ContinuousProfilerReadAllocationSamples(_buffer.Length, _buffer);
            if (read <= 0)
            {
                return;
            }

            _exportAllocationSamplesMethod(_buffer, read, cancellationToken);
        }
        catch (Exception e)
        {
            Logger.Warning(e, "Failed to process allocation samples.");
        }
    }
}
#endif
