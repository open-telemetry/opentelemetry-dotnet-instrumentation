// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using OpenTelemetry.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.OpenTelemetryProtocol.Logging;
using OpenTelemetry.OpenTelemetryProtocol.Metrics;
using OpenTelemetry.OpenTelemetryProtocol.Tracing;
using OpenTelemetry.Tracing;

namespace OpenTelemetry.OpenTelemetryProtocol;

/// <summary>
/// Implements a factory for creating OTLP exporter instances.
/// </summary>
public sealed class OtlpExporterFactory
{
    private readonly ILoggerFactory _LoggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="OtlpExporterFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/>.</param>
    public OtlpExporterFactory(
        ILoggerFactory loggerFactory)
    {
        _LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Create an OTLP log record exporter.
    /// </summary>
    /// <param name="options"><see cref="OtlpExporterOptions"/>.</param>
    /// <returns><see cref="ILogRecordExporterAsync"/>.</returns>
    public ILogRecordExporterAsync CreateOtlpLogRecordExporter(
        OtlpExporterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new OtlpLogRecordExporterAsync(
            _LoggerFactory.CreateLogger<OtlpLogRecordExporterAsync>(),
            options);
    }

    /// <summary>
    /// Create an OTLP metric exporter.
    /// </summary>
    /// <param name="options"><see cref="OtlpExporterOptions"/>.</param>
    /// <returns><see cref="IMetricExporterAsync"/>.</returns>
    public IMetricExporterAsync CreateOtlpMetricExporter(
        OtlpExporterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new OtlpMetricExporterAsync(
            _LoggerFactory.CreateLogger<OtlpMetricExporterAsync>(),
            options);
    }

    /// <summary>
    /// Create an OTLP span exporter.
    /// </summary>
    /// <param name="options"><see cref="OtlpExporterOptions"/>.</param>
    /// <returns><see cref="ISpanExporterAsync"/>.</returns>
    public ISpanExporterAsync CreateOtlpSpanExporter(
        OtlpExporterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new ProtobufOtlpSpanExporterAsync(
            _LoggerFactory.CreateLogger<ProtobufOtlpSpanExporterAsync>(),
            options);
    }
}
