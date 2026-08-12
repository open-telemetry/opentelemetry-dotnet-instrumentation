// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal class SampleExporterBuilder
{
    private readonly Dictionary<SampleType, (Action<byte[], int, uint, CancellationToken> Handler, TimeSpan ExportTimeout)>
        _sampleHandlers = new();

    private TimeSpan _exportInterval;
    private TimeSpan _exportTimeout;

    public SampleExporterBuilder AddHandler(SampleType type, Action<byte[], int, CancellationToken> handler, TimeSpan exportTimeout)
    {
        return AddHandler(type, (buffer, read, _, cancellationToken) => handler(buffer, read, cancellationToken), exportTimeout);
    }

    public SampleExporterBuilder AddHandler(SampleType type, Action<byte[], int, uint, CancellationToken> handler, TimeSpan exportTimeout)
    {
#if NET
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(exportTimeout, TimeSpan.Zero);
#else
        if (exportTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(exportTimeout));
        }
#endif
        _sampleHandlers.Add(type, (handler, exportTimeout));
        return this;
    }

    public SampleExporterBuilder SetExportInterval(TimeSpan interval)
    {
        // Prefer higher export interval.
        if (interval > _exportInterval)
        {
            _exportInterval = interval;
        }

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
        var bufferProcessor = new BufferProcessor(new Dictionary<SampleType, (Action<byte[], int, uint, CancellationToken> Handler, TimeSpan ExportTimeout)>(_sampleHandlers));
        return new SampleExporter(bufferProcessor, _exportInterval, _exportTimeout);
    }
}
