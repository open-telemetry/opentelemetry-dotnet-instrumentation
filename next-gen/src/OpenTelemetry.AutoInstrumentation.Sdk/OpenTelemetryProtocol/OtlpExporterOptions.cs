// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.OpenTelemetryProtocol;

/// <summary>
/// Contains OTLP exporter options.
/// </summary>
public sealed class OtlpExporterOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OtlpExporterOptions"/> class.
    /// </summary>
    /// <param name="protocolType"><see cref="OtlpExporterProtocolType"/>.</param>
    /// <param name="url">Url.</param>
    /// <param name="headers">Headers.</param>
    public OtlpExporterOptions(
        OtlpExporterProtocolType? protocolType,
        Uri? url,
        IReadOnlyCollection<KeyValuePair<string, string>>? headers)
    {
        ProtocolType = protocolType;
        Url = url;
        Headers = headers;
    }

    /// <summary>
    /// Gets the OTLP exporter protocol type.
    /// </summary>
    public OtlpExporterProtocolType? ProtocolType { get; }

    /// <summary>
    /// Gets the OTLP exporter url.
    /// </summary>
    public Uri? Url { get; }

    /// <summary>
    /// Gets the OTLP exporter headers.
    /// </summary>
    public IReadOnlyCollection<KeyValuePair<string, string>>? Headers { get; }
}
