// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Exporter;

namespace OpenTelemetry.AutoInstrumentation.Configurations.Otlp;

/// <summary>
/// Overrides SDK logic and sets separate values for every signal
/// based on priorities of environment variables.
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

    /// <summary>
    /// Gets the the OTLP transport protocol. Supported values: Grpc and HttpProtobuf.
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

    internal void CopySettingsTo(OtlpExporterOptions options)
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
            options.Endpoint = Endpoint;
        }

        if (TimeoutMilliseconds.HasValue)
        {
            options.TimeoutMilliseconds = TimeoutMilliseconds.Value;
        }
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
