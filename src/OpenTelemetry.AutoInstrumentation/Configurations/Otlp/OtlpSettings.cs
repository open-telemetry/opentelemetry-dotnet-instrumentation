// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.Exporter;

namespace OpenTelemetry.AutoInstrumentation.Configurations.Otlp;

/// <summary>
/// Overrides SDK logic and sets separate values for every signal
/// when more detailed environment variable is set.
/// </summary>
internal class OtlpSettings
{
    public OtlpSettings(OtlpSignalType signalType, Configuration configuration)
    {
        Protocol = GetExporterOtlpProtocol(signalType, configuration);

        var priorityVar = OtlpSpecConfigDefinitions.GetHeadersEnvVar(signalType);
        Headers = configuration.GetString(priorityVar);

        priorityVar = OtlpSpecConfigDefinitions.GetTimeoutEnvVar(signalType);
        TimeoutMilliseconds = configuration.GetInt32(priorityVar);

        priorityVar = OtlpSpecConfigDefinitions.GetEndpointEnvVar(signalType);
        Endpoint = configuration.GetUri(priorityVar);
    }

    public OtlpSettings(OtlpSignalType signalType, OtlpHttpExporterConfig configuration)
    {
        Protocol = OtlpExportProtocol.HttpProtobuf;

        Headers = CombineHeaders(configuration.Headers, configuration.HeadersList);

        TimeoutMilliseconds = configuration.Timeout;

        Endpoint = !string.IsNullOrEmpty(configuration.Endpoint) ? new Uri(configuration.Endpoint) : null;
    }

    public OtlpSettings(OtlpSignalType signalType, OtlpGrpcExporterConfig configuration)
    {
#pragma warning disable CS0618 // OtlpExportProtocol.Grpc is obsolete
        Protocol = OtlpExportProtocol.Grpc;
#pragma warning restore CS0618 // OtlpExportProtocol.Grpc is obsolete

        Headers = CombineHeaders(configuration.Headers, configuration.HeadersList);

        TimeoutMilliseconds = configuration.Timeout;

        Endpoint = !string.IsNullOrEmpty(configuration.Endpoint) ? new Uri(configuration.Endpoint) : null;
    }

    /// <summary>
    /// Gets the OTLP transport protocol. Supported values: Grpc and HttpProtobuf.
    /// </summary>
    public OtlpExportProtocol? Protocol { get; private set; }

    /// <summary>
    /// Gets the optional headers for the connection.
    /// </summary>
    public string? Headers { get; private set; }

    /// <summary>
    /// Gets the max waiting time (in milliseconds) for the backend to
    /// process each batch. Default value: <c>10000</c>.
    /// </summary>
    public int? TimeoutMilliseconds { get; private set; }

    /// <summary>
    /// Gets the target to which the exporter is going to send telemetry.
    /// </summary>
    public Uri? Endpoint { get; private set; }

    public void CopyTo(OtlpExporterOptions options)
    {
        if (Protocol.HasValue)
        {
            options.Protocol = Protocol.Value;
        }

        if (!string.IsNullOrWhiteSpace(Headers))
        {
            options.Headers = Headers;
        }

        if (Endpoint is not null)
        {
            // NOTE! This must be always full path. Endpoint setter is disabling further path handling in SDK side.
            options.Endpoint = Endpoint;
        }

        if (TimeoutMilliseconds.HasValue)
        {
            options.TimeoutMilliseconds = TimeoutMilliseconds.Value;
        }
    }

    private static string? CombineHeaders(List<Header>? headers, string? headersList)
    {
        var headerDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Parse headersList (lower priority)
        if (!string.IsNullOrWhiteSpace(headersList))
        {
            var pairs = headersList!.Split([','], StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=');
                if (parts.Length == 2)
                {
                    var key = parts[0];
                    var value = parts[1];
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    {
                        headerDict[key] = value;
                    }
                }
            }
        }

        // Override with headers (higher priority)
        if (headers != null)
        {
            foreach (var header in headers)
            {
                if (!string.IsNullOrWhiteSpace(header?.Name) && !string.IsNullOrWhiteSpace(header?.Value))
                {
                    headerDict[header!.Name!] = header!.Value!;
                }
            }
        }

        return headerDict.Count == 0
            ? null
            : string.Join(",", headerDict.Select(kvp => $"{kvp.Key}={kvp.Value}"));
    }

    private static OtlpExportProtocol? GetExporterOtlpProtocol(OtlpSignalType signalType, Configuration configuration)
    {
        // the default in SDK is grpc. http/protobuf should be default for our purposes
        var priorityVar = OtlpSpecConfigDefinitions.GetProtocolEnvVar(signalType);
        var exporterOtlpProtocol = configuration.GetString(priorityVar) ??
            configuration.GetString(OtlpSpecConfigDefinitions.DefaultProtocolEnvVarName);

        if (string.IsNullOrEmpty(exporterOtlpProtocol))
        {
            // override settings only for http/protobuf
            return OtlpExportProtocol.HttpProtobuf;
        }

        // null value here means that it will be handled by OTEL .NET SDK
        return null;
    }
}
