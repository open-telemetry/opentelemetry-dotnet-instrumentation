// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text;

namespace TestApplication.ContinuousProfiler;

public class ConsoleExporter
{
    static ConsoleExporter()
    {
        Console.OutputEncoding = Encoding.Default;
    }

    public void ExportThreadSamples(byte[] buffer, int read)
    {
        var threadSamples = SampleNativeFormatParser.ParseThreadSamples(buffer, read);

        Console.WriteLine("ExportThreadSamples: " + string.Join("\n", threadSamples!.First().Frames));
    }

    public void ExportAllocationSamples(byte[] buffer, int read)
    {
        var allocationSamples = SampleNativeFormatParser.ParseAllocationSamples(buffer, read);
        var allocationSample = allocationSamples.FirstOrDefault();
        if (allocationSample == null)
        {
            return;
        }

        Console.WriteLine("ExportAllocationSamples[" + allocationSample.AllocationSizeBytes + "]" + string.Join("\n", allocationSample.ThreadSample.Frames));
    }
}
