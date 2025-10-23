// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class SimpleTracerExporterConfig
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
    /// Gets or sets the Zipkin exporter configuration.
    /// </summary>
    [YamlMember(Alias = "zipkin")]
    public ZipkinExporterConfig? Zipkin { get; set; }

    /// <summary>
    /// Gets or sets the console exporter configuration.
    /// </summary>
    [YamlMember(Alias = "console")]
    public object? Console { get; set; }
}
