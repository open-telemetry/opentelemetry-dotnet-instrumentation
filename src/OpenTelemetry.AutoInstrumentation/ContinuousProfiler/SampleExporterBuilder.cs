// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal class SampleExporterBuilder
{
    private readonly Dictionary<SampleType, (Action<byte[], int, uint, CancellationToken> Handler, TimeSpan ExportInterval, TimeSpan ExportTimeout)>
        _sampleHandlers = new();

    private TimeSpan _exportTimeout;

    public SampleExporterBuilder AddHandler(SampleType type, Action<byte[], int, CancellationToken> handler, TimeSpan exportInterval, TimeSpan exportTimeout)
    {
        return AddHandler(type, (buffer, read, _, cancellationToken) => handler(buffer, read, cancellationToken), exportInterval, exportTimeout);
    }

    public SampleExporterBuilder AddHandler(SampleType type, Action<byte[], int, uint, CancellationToken> handler, TimeSpan exportInterval, TimeSpan exportTimeout)
    {
        _sampleHandlers.Add(type, (handler, exportInterval, exportTimeout));
        return this;
    }

    public SampleExporterBuilder SetExportTimeout(TimeSpan timeout)
    {
        // Prefer higher timeout.
        if (timeout > _exportTimeout)
        {
            _exportTimeout = timeout;
        }

        return this;
    }

    public SampleExporter Build()
    {
        return BuildCore(null);
    }

    internal SampleExporter Build(Func<SampleType, byte[], (int Read, uint SamplingInterval)> readBuffer)
    {
        return BuildCore(readBuffer);
    }

    private SampleExporter BuildCore(Func<SampleType, byte[], (int Read, uint SamplingInterval)>? readBuffer)
    {
        var handlers = new Dictionary<SampleType, (Action<byte[], int, uint, CancellationToken> Handler, TimeSpan ExportTimeout)>();
        var exportIntervals = new Dictionary<SampleType, TimeSpan>();
        foreach (var entry in _sampleHandlers)
        {
            handlers.Add(entry.Key, (entry.Value.Handler, entry.Value.ExportTimeout));
            exportIntervals.Add(entry.Key, entry.Value.ExportInterval);
        }

        var bufferProcessor = readBuffer == null
            ? new BufferProcessor(handlers)
            : new BufferProcessor(handlers, readBuffer);
        return new SampleExporter(bufferProcessor, exportIntervals, _exportTimeout);
    }
}
