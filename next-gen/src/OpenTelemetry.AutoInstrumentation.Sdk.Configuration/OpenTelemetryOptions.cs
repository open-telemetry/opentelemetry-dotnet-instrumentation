// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Configuration;

/// <summary>
/// Contains OpenTelemetry options.
/// </summary>
public sealed class OpenTelemetryOptions
{
    internal OpenTelemetryOptions(
        OpenTelemetryResourceOptions resourceOptions,
        OpenTelemetryLoggingOptions loggingOptions,
        OpenTelemetryMetricsOptions metricsOptions,
        OpenTelemetryTracingOptions tracingOptions,
        OpenTelemetryExporterOptions exporterOptions)
    {
        ResourceOptions = resourceOptions;
        LoggingOptions = loggingOptions;
        MetricsOptions = metricsOptions;
        TracingOptions = tracingOptions;
        ExporterOptions = exporterOptions;
    }

    /// <summary>
    /// Gets the OpenTelemetry resource options.
    /// </summary>
    public OpenTelemetryResourceOptions ResourceOptions { get; }

    /// <summary>
    /// Gets the OpenTelemetry logging options.
    /// </summary>
    public OpenTelemetryLoggingOptions LoggingOptions { get; }

    /// <summary>
    /// Gets the OpenTelemetry metrics options.
    /// </summary>
    public OpenTelemetryMetricsOptions MetricsOptions { get; }

    /// <summary>
    /// Gets the OpenTelemetry tracing options.
    /// </summary>
    public OpenTelemetryTracingOptions TracingOptions { get; }

    /// <summary>
    /// Gets the OpenTelemetry exporter options.
    /// </summary>
    public OpenTelemetryExporterOptions ExporterOptions { get; }
}
