// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class MetricExporterConfig
{
    /// <summary>
    /// Gets or sets the OTLP HTTP exporter configuration.
    /// </summary>
    [YamlMember(Alias = "otlp_http")]
    public OtlpHttpExporterConfig? OtlpHttp { get; set; }

    /// <summary>
    /// Gets or sets the OTLP gRPC exporter configuration.
    /// </summary>
    [YamlMember(Alias = "otlp_grpc")]
    public OtlpGrpcExporterConfig? OtlpGrpc { get; set; }

    /// <summary>
    /// Gets or sets the console exporter configuration.
    /// </summary>
    [YamlMember(Alias = "console")]
    public ConsoleMetricExporterConfig? Console { get; set; }

    /// <summary>
    /// Gets or sets the Prometheus exporter configuration.
    /// </summary>
    [YamlMember(Alias = "prometheus")]
    public PrometheusExporterConfig? Prometheus { get; set; }
}
