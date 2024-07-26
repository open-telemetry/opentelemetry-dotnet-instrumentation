// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Enum representing supported trace exporters.
/// </summary>
internal enum TracesExporter
{
    /// <summary>
    /// OTLP exporter.
    /// </summary>
    Otlp,

    /// <summary>
    /// Zipkin exporter.
    /// </summary>
    Zipkin,

    /// <summary>
    /// Console exporter.
    /// </summary>
    Console,
}
