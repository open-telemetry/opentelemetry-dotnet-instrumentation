// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal class BufferProcessor
{
    // If you change any of these constants, check with continuous_profiler.cpp first
    private const int BufferSize = 200 * 1024;

    private readonly bool _threadSamplingEnabled;
    private readonly bool _allocationSamplingEnabled;
    private readonly Action<byte[], int> _exportThreadSamplesMethod;
    private readonly Action<byte[], int> _exportAllocationSamplesMethod;
    private readonly byte[] _buffer = new byte[BufferSize];

    public BufferProcessor(bool threadSamplingEnabled, bool allocationSamplingEnabled, Action<byte[], int> threadSamplesMethod, Action<byte[], int> allocationSamplesMethod)
    {
        _threadSamplingEnabled = threadSamplingEnabled;
        _allocationSamplingEnabled = allocationSamplingEnabled;
        _exportThreadSamplesMethod = threadSamplesMethod;
        _exportAllocationSamplesMethod = allocationSamplesMethod;
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
        var read = NativeMethods.ContinuousProfilerReadThreadSamples(_buffer.Length, _buffer);
        if (read <= 0)
        {
            return;
        }

        _exportThreadSamplesMethod(_buffer, read);
    }

    private void ProcessAllocationSamples()
    {
        var read = NativeMethods.ContinuousProfilerReadAllocationSamples(_buffer.Length, _buffer);
        if (read <= 0)
        {
            return;
        }

        _exportAllocationSamplesMethod(_buffer, read);
    }
}
#endif
