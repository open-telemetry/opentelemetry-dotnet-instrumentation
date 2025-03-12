// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using OpenTelemetry.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.OpenTelemetryProtocol;
using OpenTelemetry.Resources;
using OpenTelemetry.Tracing;

namespace OpenTelemetry.Configuration;

/// <summary>
/// Contains methods for instantiating OpenTelemetry SDK components from options.
/// </summary>
public static class OpenTelemetryFactory
{
    /// <summary>
    /// Create an OpenTelemetry <see cref="Resource"/> from options.
    /// </summary>
    /// <param name="resourceOptions"><see cref="OpenTelemetryResourceOptions"/>.</param>
    /// <param name="unresolvedAttributes">Contains resource attributes which could not be resolved.</param>
    /// <param name="environmentVariables">Environment variables to use for resolution.</param>
    /// <param name="serviceName">Service name value.</param>
    /// <param name="serviceInstanceId">Service instance identifier value.</param>
    /// <returns><see cref="Resource"/>.</returns>
    public static Resource CreateResource(
        OpenTelemetryResourceOptions resourceOptions,
        out IReadOnlyList<OpenTelemetryResourceAttributeOptions> unresolvedAttributes,
        IDictionary<string, string>? environmentVariables = null,
        string? serviceName = null,
        string? serviceInstanceId = null)
    {
        ArgumentNullException.ThrowIfNull(resourceOptions);

        var unresolvedAttributeList = new List<OpenTelemetryResourceAttributeOptions>();
        var resourceAttributes = new Dictionary<string, object>();

        foreach (var resourceAttribute in resourceOptions.AttributeOptions)
        {
            if (resourceAttribute.ValueOrExpression.StartsWith("$env:", StringComparison.OrdinalIgnoreCase))
            {
                string key = resourceAttribute.ValueOrExpression.Substring(5);
                if (environmentVariables != null
                    && environmentVariables.TryGetValue(key, out string? value)
                    && !string.IsNullOrEmpty(value))
                {
                    resourceAttributes.Add(resourceAttribute.Key, value);
                }
                else
                {
                    unresolvedAttributeList.Add(resourceAttribute);
                }
            }
            else
            {
                resourceAttributes.Add(resourceAttribute.Key, resourceAttribute.ValueOrExpression);
            }
        }

        if (!resourceAttributes.ContainsKey("service.name") && !string.IsNullOrEmpty(serviceName))
        {
            resourceAttributes.Add("service.name", serviceName);
        }

        if (!resourceAttributes.ContainsKey("service.instance.id") && !string.IsNullOrEmpty(serviceInstanceId))
        {
            resourceAttributes.Add("service.instance.id", serviceInstanceId);
        }

        unresolvedAttributes = unresolvedAttributeList.AsReadOnly();

        return new Resource(resourceAttributes);
    }

    /// <summary>
    /// Create an OpenTelemetry batch export <see cref="ILogRecordProcessor"/> from options.
    /// </summary>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/>.</param>
    /// <param name="resource"><see cref="Resource"/>.</param>
    /// <param name="exporterOptions"><see cref="OpenTelemetryExporterOptions"/>.</param>
    /// <param name="batchOptions"><see cref="OpenTelemetryBatchOptions"/>.</param>
    /// <returns><see cref="ILogRecordProcessor"/>.</returns>
    public static ILogRecordProcessor CreateLogRecordBatchExportProcessorAsync(
        ILoggerFactory loggerFactory,
        Resource resource,
        OpenTelemetryExporterOptions exporterOptions,
        OpenTelemetryBatchOptions batchOptions)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(exporterOptions);
        ArgumentNullException.ThrowIfNull(batchOptions);

        if (exporterOptions.ExporterType != "OpenTelemetryProtocol"
            || exporterOptions.OpenTelemetryProtocolExporterOptions == null)
        {
            throw new NotSupportedException($"ExporterType '{exporterOptions.ExporterType}' is not supported.");
        }

        var factory = new OtlpExporterFactory(loggerFactory);

        var otlpExporterOptions = exporterOptions.OpenTelemetryProtocolExporterOptions.ResolveOtlpExporterOptions(
            new Uri("http://localhost:4318/v1/logs"),
            exporterOptions.OpenTelemetryProtocolExporterOptions.LoggingOptions);

        return LogRecordExportProcessorFactory.CreateBatchExportProcessorAsync(
            loggerFactory,
            resource,
            factory.CreateOtlpLogRecordExporter(otlpExporterOptions),
            batchOptions.ToOTelBatchOptions());
    }

    /// <summary>
    /// Create an OpenTelemetry batch export <see cref="ISpanProcessor"/> from options.
    /// </summary>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/>.</param>
    /// <param name="resource"><see cref="Resource"/>.</param>
    /// <param name="exporterOptions"><see cref="OpenTelemetryExporterOptions"/>.</param>
    /// <param name="batchOptions"><see cref="OpenTelemetryBatchOptions"/>.</param>
    /// <returns><see cref="ISpanProcessor"/>.</returns>
    public static ISpanProcessor CreateSpanBatchExportProcessorAsync(
        ILoggerFactory loggerFactory,
        Resource resource,
        OpenTelemetryExporterOptions exporterOptions,
        OpenTelemetryBatchOptions batchOptions)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(exporterOptions);
        ArgumentNullException.ThrowIfNull(batchOptions);

        if (exporterOptions.ExporterType != "OpenTelemetryProtocol"
            || exporterOptions.OpenTelemetryProtocolExporterOptions == null)
        {
            throw new NotSupportedException($"ExporterType '{exporterOptions.ExporterType}' is not supported.");
        }

        var factory = new OtlpExporterFactory(loggerFactory);

        var otlpExporterOptions = exporterOptions.OpenTelemetryProtocolExporterOptions.ResolveOtlpExporterOptions(
            new Uri("http://localhost:4318/v1/traces"),
            exporterOptions.OpenTelemetryProtocolExporterOptions.TracingOptions);

        return SpanExportProcessorFactory.CreateBatchExportProcessorAsync(
            loggerFactory,
            resource,
            factory.CreateOtlpSpanExporter(otlpExporterOptions),
            batchOptions.ToOTelBatchOptions());
    }

    /// <summary>
    /// Create an OpenTelemetry periodic exporting <see cref="IMetricReader"/> from options.
    /// </summary>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/>.</param>
    /// <param name="resource"><see cref="Resource"/>.</param>
    /// <param name="exporterOptions"><see cref="OpenTelemetryExporterOptions"/>.</param>
    /// <param name="periodicExportingOptions"><see cref="OpenTelemetryPeriodicExportingOptions"/>.</param>
    /// <param name="metricProducerFactories"><see cref="IMetricProducerFactory"/> used to create <see cref="MetricProducer"/>s for the metric reader.</param>
    /// <returns><see cref="IMetricReader"/>.</returns>
    public static IMetricReader CreatePeriodicExportingMetricReaderAsync(
        ILoggerFactory loggerFactory,
        Resource resource,
        OpenTelemetryExporterOptions exporterOptions,
        OpenTelemetryPeriodicExportingOptions periodicExportingOptions,
        IEnumerable<IMetricProducerFactory> metricProducerFactories)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(exporterOptions);
        ArgumentNullException.ThrowIfNull(periodicExportingOptions);
        ArgumentNullException.ThrowIfNull(metricProducerFactories);

        if (exporterOptions.ExporterType != "OpenTelemetryProtocol"
            || exporterOptions.OpenTelemetryProtocolExporterOptions == null)
        {
            throw new NotSupportedException($"ExporterType '{exporterOptions.ExporterType}' is not supported.");
        }

        var factory = new OtlpExporterFactory(loggerFactory);

        var otlpExporterOptions = exporterOptions.OpenTelemetryProtocolExporterOptions.ResolveOtlpExporterOptions(
            new Uri("http://localhost:4318/v1/metrics"),
            exporterOptions.OpenTelemetryProtocolExporterOptions.MetricsOptions);

        return MetricReaderFactory.CreatePeriodicExportingMetricReaderAsync(
            loggerFactory,
            resource,
            factory.CreateOtlpMetricExporter(otlpExporterOptions),
            metricProducerFactories,
            periodicExportingOptions.ToOTelPeriodicExportingOptions());
    }
}
