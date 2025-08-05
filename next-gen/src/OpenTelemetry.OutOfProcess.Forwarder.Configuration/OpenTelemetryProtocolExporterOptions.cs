// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;
using OpenTelemetry.OpenTelemetryProtocol;

namespace OpenTelemetry.Configuration;

/// <summary>
/// Contains OpenTelemetry OTLP exporter options.
/// </summary>
public sealed class OpenTelemetryProtocolExporterOptions
{
    internal static OpenTelemetryProtocolExporterOptions ParseFromConfig(IConfigurationSection config)
    {
        Debug.Assert(config != null);

        return new(
            OpenTelemetryProtocolExporterSignalOptions.ParseFromConfig(config.GetSection("Defaults")),
            OpenTelemetryProtocolExporterSignalOptions.ParseFromConfig(config.GetSection("Logs")),
            OpenTelemetryProtocolExporterSignalOptions.ParseFromConfig(config.GetSection("Metrics")),
            OpenTelemetryProtocolExporterSignalOptions.ParseFromConfig(config.GetSection("Traces")));
    }

    private static Uri ResolveUrl(
        OpenTelemetryProtocolExporterSignalOptions signalOptions,
        OpenTelemetryProtocolExporterSignalOptions defaultOptions,
        Uri defaultUri)
    {
        return signalOptions.Url
            ?? AppendPathToUrl(defaultOptions.Url, defaultUri.PathAndQuery)
            ?? defaultUri;
    }

    [return: NotNullIfNotNull(nameof(uri))]
    private static Uri? AppendPathToUrl(Uri? uri, string path)
    {
        if (uri == null)
        {
            return null;
        }

        string absoluteUri = uri.AbsoluteUri;

        if (!absoluteUri.EndsWith('/'))
        {
            absoluteUri += "/";
        }

        if (path.StartsWith('/'))
        {
            path = path.Substring(1);
        }

        return new Uri(string.Concat(absoluteUri, path));
    }

    internal OpenTelemetryProtocolExporterOptions(
        OpenTelemetryProtocolExporterSignalOptions defaultOptions,
        OpenTelemetryProtocolExporterSignalOptions loggingOptions,
        OpenTelemetryProtocolExporterSignalOptions metricsOptions,
        OpenTelemetryProtocolExporterSignalOptions tracingOptions)
    {
        Debug.Assert(defaultOptions != null);
        Debug.Assert(loggingOptions != null);
        Debug.Assert(metricsOptions != null);
        Debug.Assert(tracingOptions != null);

        DefaultOptions = defaultOptions;
        LoggingOptions = loggingOptions;
        MetricsOptions = metricsOptions;
        TracingOptions = tracingOptions;
    }

    /// <summary>
    /// Gets the OTLP exporter default options.
    /// </summary>
    public OpenTelemetryProtocolExporterSignalOptions DefaultOptions { get; }

    /// <summary>
    /// Gets the OTLP exporter logging options.
    /// </summary>
    public OpenTelemetryProtocolExporterSignalOptions LoggingOptions { get; }

    /// <summary>
    /// Gets the OTLP exporter metrics options.
    /// </summary>
    public OpenTelemetryProtocolExporterSignalOptions MetricsOptions { get; }

    /// <summary>
    /// Gets the OTLP exporter tracing options.
    /// </summary>
    public OpenTelemetryProtocolExporterSignalOptions TracingOptions { get; }

    internal OtlpExporterOptions ResolveOtlpExporterOptions(
        Uri defaultUri,
        OpenTelemetryProtocolExporterSignalOptions signalOptions)
    {
        Uri requestUri = ResolveUrl(signalOptions, DefaultOptions, defaultUri);
        OtlpExporterProtocolType protocol = signalOptions.ProtocolType ?? DefaultOptions.ProtocolType ?? OtlpExporterProtocolType.HttpProtobuf;
        IReadOnlyCollection<OpenTelemetryProtocolExporterHeaderOptions>? headers = signalOptions.HeaderOptions ?? DefaultOptions.HeaderOptions;

        return new OtlpExporterOptions(
            (OtlpExporterProtocolType)(int)protocol,
            requestUri,
            headers?.Select(h => new KeyValuePair<string, string>(h.Key, h.Value)).ToList());
    }
}
