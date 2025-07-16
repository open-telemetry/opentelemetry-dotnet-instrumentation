// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using OpenTelemetry.Resources;
using TestApplication.ContinuousProfiler;

namespace TestApplication.SelectiveSampler.Plugins;

public class SelectiveSamplerPlugin
{
    public Tuple<uint, TimeSpan, TimeSpan, object> GetSelectiveSamplingConfiguration()
    {
        var samplingInterval = 50u;
        return Tuple.Create<uint, TimeSpan, TimeSpan, object>(samplingInterval, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(5000), new JsonConsoleExporter());
    }

    public ResourceBuilder ConfigureResource(ResourceBuilder builder)
    {
        ResourcesProvider.Configure(builder);
        return builder;
    }

    private class JsonConsoleExporter
    {
        public void ExportSelectedThreadSamples(byte[] buffer, int read, CancellationToken cancellationToken)
        {
            var samples = SampleNativeFormatParser.ParseSelectiveSamplerSamples(buffer, read);
            var serialized = JsonSerializer.Serialize(samples);
            Console.WriteLine(serialized);
            Console.WriteLine();
        }
    }
}
