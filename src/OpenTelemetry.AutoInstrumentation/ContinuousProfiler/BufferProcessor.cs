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

    private readonly bool _threadSamplingEnabled;
    private readonly bool _allocationSamplingEnabled;
    private readonly Action<byte[], int, CancellationToken> _exportThreadSamplesMethod;
    private readonly Action<byte[], int, CancellationToken> _exportAllocationSamplesMethod;
    private readonly TimeSpan _exportTimeout;
    private readonly byte[] _buffer = new byte[BufferSize];

    public BufferProcessor(
        bool threadSamplingEnabled,
        bool allocationSamplingEnabled,
        Action<byte[], int, CancellationToken> threadSamplesMethod,
        Action<byte[], int, CancellationToken> allocationSamplesMethod,
        TimeSpan exportTimeout)
    {
        _threadSamplingEnabled = threadSamplingEnabled;
        _allocationSamplingEnabled = allocationSamplingEnabled;
        _exportThreadSamplesMethod = threadSamplesMethod;
        _exportAllocationSamplesMethod = allocationSamplesMethod;
        if (exportTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(exportTimeout));
        }

        _exportTimeout = exportTimeout;
    }

    public void Process()
    {
        if (_threadSamplingEnabled)
        {
            ProcessThreadSamples();
        }

        if (_allocationSamplingEnabled)
        {
            ProcessAllocationSamples();
        }
    }

    private void ProcessThreadSamples()
    {
        try
        {
            var read = NativeMethods.ContinuousProfilerReadThreadSamples(_buffer.Length, _buffer);
            if (read <= 0)
            {
                return;
            }

            using var cts = new CancellationTokenSource(_exportTimeout);
            _exportThreadSamplesMethod(_buffer, read, cts.Token);
        }
        catch (Exception e)
        {
            Logger.Warning(e, "Failed to process thread samples.");
        }
    }

    private void ProcessAllocationSamples()
    {
        try
        {
            var read = NativeMethods.ContinuousProfilerReadAllocationSamples(_buffer.Length, _buffer);
            if (read <= 0)
            {
                return;
            }

            using var cts = new CancellationTokenSource(_exportTimeout);
            _exportAllocationSamplesMethod(_buffer, read, cts.Token);
        }
        catch (Exception e)
        {
            Logger.Warning(e, "Failed to process allocation samples.");
        }
    }
}
#endif
