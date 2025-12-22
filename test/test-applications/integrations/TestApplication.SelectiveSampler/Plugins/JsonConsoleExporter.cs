// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using TestApplication.ContinuousProfiler;

namespace TestApplication.SelectiveSampler.Plugins;

internal sealed class JsonConsoleExporter
{
    private readonly SampleNativeFormatParser _sampleNativeFormatParser;

    private JsonConsoleExporter(SampleNativeFormatParser sampleNativeFormatParser)
    {
        _sampleNativeFormatParser = sampleNativeFormatParser;
    }

    public static JsonConsoleExporter Instance { get; } = new JsonConsoleExporter(new SampleNativeFormatParser(true));

    public void ExportSelectedThreadSamples(byte[] buffer, int read, CancellationToken cancellationToken)
    {
        var samples = _sampleNativeFormatParser.ParseSelectiveSamplerSamples(buffer, read);
        WriteToConsole(samples);
    }

    public void ExportThreadSamples(byte[] buffer, int read, CancellationToken cancellationToken)
    {
        var samples = _sampleNativeFormatParser.ParseThreadSamples(buffer, read);
        WriteToConsole(samples ?? []);
    }

    public void ExportAllocationSamples(byte[] buffer, int read, CancellationToken cancellationToken)
    {
    }

    private static void WriteToConsole(List<ThreadSample> samples)
    {
        if (samples.Count <= 0)
        {
            return;
        }

        var serialized = JsonSerializer.Serialize(samples);
        Console.WriteLine(serialized);
        Console.WriteLine();
    }
}
