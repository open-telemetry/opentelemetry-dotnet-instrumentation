// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER

using OpenTelemetry.Exporter;

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal class OtlpOverHttpExporter
{
    private readonly OtlpProfilerExporter _exporter;

    public OtlpOverHttpExporter()
    {
        var transformer = new OtlpProfilerTransformer();
        _exporter = new OtlpProfilerExporter(new OtlpExporterOptions { Protocol = OtlpExportProtocol.HttpProtobuf }, transformer);
    }

    public void ExportThreadSamples(byte[] buffer, int read, CancellationToken cancellationToken)
    {
        var exportData = Tuple.Create(buffer, read, "cpu");
        Console.WriteLine("XXXXX-exporting CPU-begin");
        _exporter.Export(new Batch<Tuple<byte[], int, string>>(new[] { exportData }, 1));
        Console.WriteLine("XXXXX-exporting CPU-end");
    }

    public void ExportAllocationSamples(byte[] buffer, int read, CancellationToken cancellationToken)
    {
        var exportData = Tuple.Create(buffer, read, "memory");
        Console.WriteLine("XXXXX-exporting memory-begin");
        _exporter.Export(new Batch<Tuple<byte[], int, string>>(new[] { exportData }, 1));
        Console.WriteLine("XXXXX-exporting memory-end");
    }
}
#endif
