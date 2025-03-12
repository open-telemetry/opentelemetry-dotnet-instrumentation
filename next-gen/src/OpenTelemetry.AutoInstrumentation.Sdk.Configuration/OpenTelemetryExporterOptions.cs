// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;

namespace OpenTelemetry.Configuration;

/// <summary>
/// Contains OpenTelemetry exporter options.
/// </summary>
public sealed class OpenTelemetryExporterOptions
{
    internal static OpenTelemetryExporterOptions ParseFromConfig(IConfigurationSection config)
    {
        Debug.Assert(config != null);

        string? exporterTypeValue = config["Type"];

        string exporterType = "Unknown";
        OpenTelemetryProtocolExporterOptions? openTelemetryProtocolExporterOptions = null;
        IConfigurationSection exporterConfiguration = config.GetSection("Settings");

        if (string.Equals(exporterTypeValue, "otlp", StringComparison.OrdinalIgnoreCase))
        {
            exporterType = "OpenTelemetryProtocol";
            openTelemetryProtocolExporterOptions = OpenTelemetryProtocolExporterOptions.ParseFromConfig(
                exporterConfiguration);
        }

        return new(exporterType, exporterConfiguration, openTelemetryProtocolExporterOptions);
    }

    internal OpenTelemetryExporterOptions(
        string exporterType,
        IConfigurationSection exporterConfiguration,
        OpenTelemetryProtocolExporterOptions? openTelemetryProtocolExporterOptions)
    {
        Debug.Assert(!string.IsNullOrEmpty(exporterType));
        Debug.Assert(exporterConfiguration != null);

        ExporterType = exporterType;
        ExporterConfiguration = exporterConfiguration;
        OpenTelemetryProtocolExporterOptions = openTelemetryProtocolExporterOptions;
    }

    /// <summary>
    /// Gets the exporter type.
    /// </summary>
    public string ExporterType { get; }

    /// <summary>
    /// Gets the exporter configuration.
    /// </summary>
    public IConfigurationSection ExporterConfiguration { get; }

    /// <summary>
    /// Gets the OTLP exporter options.
    /// </summary>
    public OpenTelemetryProtocolExporterOptions? OpenTelemetryProtocolExporterOptions { get; }
}
