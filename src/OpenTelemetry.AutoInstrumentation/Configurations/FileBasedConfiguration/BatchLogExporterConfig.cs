// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class BatchLogExporterConfig
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
}
