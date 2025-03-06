// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.OpenTelemetryProtocol;

/// <summary>
/// Describes the supported OTLP exporter protocols.
/// </summary>
public enum OtlpExporterProtocolType
{
    /// <summary>
    /// Unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// OTLP over gRPC.
    /// </summary>
    Grpc = 1,

    /// <summary>
    /// OTLP over HTTP with protobuf payloads.
    /// </summary>
    HttpProtobuf = 2,
}
