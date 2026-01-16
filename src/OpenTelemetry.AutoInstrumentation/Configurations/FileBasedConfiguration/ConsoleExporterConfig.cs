// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using OpenTelemetry.Metrics;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

[EmptyObjectOnEmptyYaml]
internal class ConsoleExporterConfig
{
    /// <summary>
    /// Gets or sets the temporality preference for the exporter.
    /// Values include: cumulative, delta.
    /// If omitted or null, cumulative is used.
    /// </summary>
    [YamlMember(Alias = "temporality_preference")]
    public string? TemporalityPreference { get; set; }

    public MetricReaderTemporalityPreference GetTemporalityPreference()
    {
        return TemporalityPreference switch
        {
            "cumulative" => MetricReaderTemporalityPreference.Cumulative,
            "delta" => MetricReaderTemporalityPreference.Delta,
            _ => MetricReaderTemporalityPreference.Cumulative
        };
    }
}
