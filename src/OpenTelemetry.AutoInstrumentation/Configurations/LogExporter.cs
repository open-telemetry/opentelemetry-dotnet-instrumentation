// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Enum representing supported log exporters.
/// </summary>
internal enum LogExporter
{
    /// <summary>
    /// None exporter.
    /// </summary>
    None,

    /// <summary>
    /// OTLP exporter.
    /// </summary>
    Otlp,
}
