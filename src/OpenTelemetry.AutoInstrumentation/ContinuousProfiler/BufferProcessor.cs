// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER

using System.Reflection;

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal class BufferProcessor
{
    // If you change any of these constants, check with continuous_profiler.cpp first
    private const int BufferSize = 200 * 1024;

    private readonly bool _threadSamplingEnabled;
    private readonly bool _allocationSamplingEnabled;
    private readonly object _continuousProfilerExporter;
    private readonly MethodInfo _exportThreadSamplesMethod;
    private readonly MethodInfo _exportAllocationSamplesMethod;

    public BufferProcessor(bool threadSamplingEnabled, bool allocationSamplingEnabled, object continuousProfilerExporter, MethodInfo exportThreadSamplesMethod, MethodInfo exportAllocationSamplesMethod)
    {
        _threadSamplingEnabled = threadSamplingEnabled;
        _allocationSamplingEnabled = allocationSamplingEnabled;
        _continuousProfilerExporter = continuousProfilerExporter;
        _exportThreadSamplesMethod = exportThreadSamplesMethod;
        _exportAllocationSamplesMethod = exportAllocationSamplesMethod;
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
        var buffer = new byte[BufferSize];
        var read = NativeMethods.ContinuousProfilerReadThreadSamples(buffer.Length, buffer);
        if (read <= 0)
        {
            return;
        }

        _exportThreadSamplesMethod.Invoke(_continuousProfilerExporter, new object[] { buffer, read });
    }

    private void ProcessAllocationSamples()
    {
        var buffer = new byte[BufferSize];
        var read = NativeMethods.ContinuousProfilerReadAllocationSamples(buffer.Length, buffer);
        if (read <= 0)
        {
            return;
        }

        _exportAllocationSamplesMethod.Invoke(_continuousProfilerExporter, new object[] { buffer, read });
    }
}
#endif
