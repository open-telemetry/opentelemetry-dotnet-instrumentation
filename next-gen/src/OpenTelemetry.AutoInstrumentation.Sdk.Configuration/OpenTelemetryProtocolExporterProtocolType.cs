// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Configuration;

/// <summary>
/// Defines the supported OTLP exporter protocol types.
/// </summary>
public enum OpenTelemetryProtocolExporterProtocolType
{
    /// <summary>
    /// Unknown.
    /// </summary>
    Unknown,

    /// <summary>
    /// OTLP over HTTP with JSON payloads.
    /// </summary>
    HttpProtobuf
}
