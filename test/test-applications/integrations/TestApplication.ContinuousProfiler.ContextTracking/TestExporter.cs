// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using System.Text.Json;

namespace TestApplication.ContinuousProfiler.ContextTracking;

public class TestExporter
{
    static TestExporter()
    {
        Console.OutputEncoding = Encoding.Default;
    }

    public void ExportThreadSamples(byte[] buffer, int read)
    {
        var threadSamples = SampleNativeFormatParser.ParseThreadSamples(buffer, read);

        var value = JsonSerializer.Serialize(threadSamples, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
        Console.WriteLine(value);
        Console.WriteLine();
    }

    public void ExportAllocationSamples(byte[] buffer, int read)
    {
    }
}
