// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Enum representing supported metrics exporters.
/// </summary>
internal enum MetricsExporter
{
    /// <summary>
    /// OTLP exporter.
    /// </summary>
    Otlp,

    /// <summary>
    /// Prometheus exporter.
    /// </summary>
    Prometheus,

    /// <summary>
    /// Console exporter.
    /// </summary>
    Console,
}
