// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;

namespace OpenTelemetry.Configuration;

/// <summary>
/// Contains OpenTelemetry OTLP exporter signal options.
/// </summary>
public sealed class OpenTelemetryProtocolExporterSignalOptions
{
    internal static OpenTelemetryProtocolExporterSignalOptions ParseFromConfig(IConfigurationSection config)
    {
        Debug.Assert(config != null);

        OpenTelemetryProtocolExporterProtocolType? protocol = null;
        Uri? url = null;

        string? protocolValue = config["Protocol"];
        if (!string.IsNullOrEmpty(protocolValue)
            && Enum.TryParse(protocolValue, ignoreCase: true, out OpenTelemetryProtocolExporterProtocolType tempProtocol))
        {
            protocol = tempProtocol;
        }

        string? urlValue = config["BaseUrl"];
        if (string.IsNullOrEmpty(urlValue))
        {
            urlValue = config["Url"];
        }

        if (!string.IsNullOrEmpty(urlValue)
            && Uri.TryCreate(urlValue, UriKind.Absolute, out Uri? tempUrl))
        {
            url = tempUrl;
        }

        List<OpenTelemetryProtocolExporterHeaderOptions>? headers = null;

        foreach (KeyValuePair<string, string?> header in config.GetSection("Headers").AsEnumerable(makePathsRelative: true))
        {
            if (string.IsNullOrEmpty(header.Key)
                || string.IsNullOrEmpty(header.Value))
            {
                continue;
            }

            (headers ??= new()).Add(new(header.Key, header.Value));
        }

        return new(protocol, url, headers);
    }

    internal OpenTelemetryProtocolExporterSignalOptions(
        OpenTelemetryProtocolExporterProtocolType? protocolType,
        Uri? url,
        IReadOnlyCollection<OpenTelemetryProtocolExporterHeaderOptions>? headerOptions)
    {
        ProtocolType = protocolType;
        Url = url;
        HeaderOptions = headerOptions;
    }

    /// <summary>
    /// Gets the exporter protocol type.
    /// </summary>
    public OpenTelemetryProtocolExporterProtocolType? ProtocolType { get; }

    /// <summary>
    /// Gets the exporter url.
    /// </summary>
    public Uri? Url { get; }

    /// <summary>
    /// Gets the exporter header options.
    /// </summary>
    public IReadOnlyCollection<OpenTelemetryProtocolExporterHeaderOptions>? HeaderOptions { get; }
}
