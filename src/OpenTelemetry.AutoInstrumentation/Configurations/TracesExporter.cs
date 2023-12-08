// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Enum representing supported trace exporters.
/// </summary>
internal enum TracesExporter
{
    /// <summary>
    /// None exporter.
    /// </summary>
    None,

    /// <summary>
    /// OTLP exporter.
    /// </summary>
    Otlp,

    /// <summary>
    /// Zipkin exporter.
    /// </summary>
    Zipkin,
}
