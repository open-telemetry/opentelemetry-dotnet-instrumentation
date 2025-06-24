// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class OtlpGrpcExporterConfig
{
    /// <summary>
    /// Gets or sets the endpoint for the OTLP gRPC exporter.
    /// Configure endpoint.
    /// If omitted or null, http://localhost:4317 is used.
    /// </summary>
    [YamlMember(Alias = "endpoint")]
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the certificate file used to verify a server's TLS credentials.
    /// Absolute path to certificate file in PEM format.
    /// If omitted or null, system default certificate verification is used for secure connections.
    /// </summary>
    [YamlMember(Alias = "certificate_file")]
    public string? CertificateFile { get; set; }

    /// <summary>
    /// Gets or sets the mTLS private client key.
    /// Absolute path to client key file in PEM format. If set, <c>client_certificate_file</c> must also be set.
    /// If omitted or null, mTLS is not used.
    /// </summary>
    [YamlMember(Alias = "client_key_file")]
    public string? ClientKeyFile { get; set; }

    /// <summary>
    /// Gets or sets the mTLS client certificate.
    /// Absolute path to client certificate file in PEM format. If set, <c>client_key_file</c> must also be set.
    /// If omitted or null, mTLS is not used.
    /// </summary>
    [YamlMember(Alias = "client_certificate_file")]
    public string? ClientCertificateFile { get; set; }

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
    /// Gets or sets the compression algorithm for the exporter.
    /// Values include: gzip, none. Implementations may support other compression algorithms.
    /// If omitted or null, none is used.
    /// </summary>
    [YamlMember(Alias = "compression")]
    public string? Compression { get; set; }

    /// <summary>
    /// Gets or sets the maximum time (in milliseconds) to wait for each export.
    /// Value must be non-negative. A value of 0 indicates no limit (infinity).
    /// If omitted or null, 10000 is used.
    /// </summary>
    [YamlMember(Alias = "timeout")]
    public int? Timeout { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the encoding for the exporter.
    /// Supported values include: json, protobuf. If omitted or null, protobuf is used.
    /// </summary>
    [YamlMember(Alias = "encoding")]
    public string? Encoding { get; set; }

    /// <summary>
    /// Gets or sets the temporality preference for the exporter.
    /// Supported values include: cumulative, delta. If omitted or null, cumulative is used.
    /// </summary>
    [YamlMember(Alias = "temporality_preference")]
    public string? TemporalityPreference { get; set; }

    /// <summary>
    /// Gets or sets the default histogram aggregation for the exporter.
    /// Supported values include: explicit_bucket_histogram, base2_exponential_bucket_histogram.
    /// If omitted or null, explicit_bucket_histogram is used.
    /// </summary>
    [YamlMember(Alias = "default_histogram_aggregation")]
    public string? DefaultHistogramAggregation { get; set; }
}
