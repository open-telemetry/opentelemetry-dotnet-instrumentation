// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ContinuousProfiler;

public class ConsoleExporter
{
    public void ExportThreadSamples(byte[] buffer, int read)
    {
        var threadSamples = SampleNativeFormatParser.ParseThreadSamples(buffer, read);

        Console.WriteLine("ExportThreadSamples: " + string.Join("\r\n", threadSamples!.First().Frames));
    }

    public void ExportAllocationSamples(byte[] buffer, int read)
    {
        var allocationSamples = SampleNativeFormatParser.ParseAllocationSamples(buffer, read);
        Console.WriteLine("ExportAllocationSamples: " + string.Join("\r\n", allocationSamples!.First().ThreadSample.Frames));
    }
}
