// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using OpenTelemetry.Metrics;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

[EmptyObjectOnEmptyYaml]
internal class OtlpGrpcExporterConfig
{
    /// <summary>
    /// Gets or sets the endpoint for the OTLP gRPC exporter.
    /// Configure endpoint.
    /// If omitted or null, http://localhost:4317 is used.
    /// </summary>
    [YamlMember(Alias = "endpoint")]
    public string Endpoint { get; set; } = "http://localhost:4317";

    /// <summary>
    /// Gets or sets the headers for the exporter.
    /// Configure headers. Entries have higher priority than entries from <c>headers_list</c>.
    /// If an entry's value is null, the entry is ignored.
    /// </summary>
    [YamlMember(Alias = "headers")]
    public List<Header>? Headers { get; set; }

    /// <summary>
    /// Gets or sets the headers list for the exporter.
    /// Configure headers. Entries have lower priority than entries from <c>headers</c>.
    /// The value is a list of comma separated key-value pairs matching the format of OTEL_EXPORTER_OTLP_HEADERS.
    /// If omitted or null, no headers are added.
    /// </summary>
    [YamlMember(Alias = "headers_list")]
    public string? HeadersList { get; set; }

    /// <summary>
    /// Gets or sets the maximum time (in milliseconds) to wait for each export.
    /// Value must be non-negative. A value of 0 indicates no limit (infinity).
    /// If omitted or null, 10000 is used.
    /// </summary>
    [YamlMember(Alias = "timeout")]
    public int? Timeout { get; set; } = 10000;

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
