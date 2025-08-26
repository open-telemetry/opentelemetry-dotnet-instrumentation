// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using System.Text.Json;

namespace IntegrationTests;

internal static class ConsoleProfileExporterHelpers
{
    public static List<ConsoleThreadSample> ExtractSamples(string output)
    {
        var batchSeparator = $"{Environment.NewLine}{Environment.NewLine}";
        var lines = output.Split(batchSeparator);
        var deserializedSampleBatches = lines[..^1].Select(sample => JsonSerializer.Deserialize<List<ConsoleThreadSample>>(sample)).ToList();

        var threadSamples = new List<ConsoleThreadSample>();
        foreach (var batch in deserializedSampleBatches)
        {
            threadSamples.AddRange(batch!);
        }

        return threadSamples;
    }
}

#endif
