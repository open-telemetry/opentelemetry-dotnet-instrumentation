// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Enum representing supported log exporters.
/// </summary>
internal enum LogExporter
{
    /// <summary>
    /// OTLP exporter.
    /// </summary>
    Otlp,

    /// <summary>
    /// Console exporter.
    /// </summary>
    Console,
}
