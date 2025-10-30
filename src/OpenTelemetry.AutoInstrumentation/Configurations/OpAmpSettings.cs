// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.OpAmp.Client.Settings;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal class OpAmpSettings : Settings
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    /// <summary>
    /// Gets a value indicating whether the OpAmp client is enabled.
    /// </summary>
    public bool OpAmpClientEnabled { get; private set; }

    /// <summary>
    /// Gets the URL of the server to which the application connects.
    /// </summary>
    public Uri? ServerUrl { get; private set; }

    /// <summary>
    /// Gets the type of connection used for communication.
    /// </summary>
    public ConnectionType? ServerConnectionType { get; private set; }

    protected override void OnLoadEnvVar(Configuration configuration)
    {
        OpAmpClientEnabled = configuration.GetBool(ConfigurationKeys.OpAmpEnabled) ?? false;
        ServerUrl = GetServerUrl(configuration.GetString(ConfigurationKeys.OpAmpServerUrl), configuration.FailFast);
        ServerConnectionType = GetConnectionType(configuration.GetString(ConfigurationKeys.OpAmpConnectionType), configuration.FailFast);
    }

    protected override void OnLoadFile(YamlConfiguration configuration)
    {
        OpAmpClientEnabled = configuration.OpAmp?.Enabled ?? false;
        ServerUrl = GetServerUrl(configuration.OpAmp?.ServerUrl, configuration.FailFast);
        ServerConnectionType = GetConnectionType(configuration.OpAmp?.ConnectionType, configuration.FailFast);
    }

    private static Uri? GetServerUrl(string? configurationValue, bool failFast)
    {
        if (string.IsNullOrWhiteSpace(configurationValue))
        {
            // indicates that the default value should be used
            return null;
        }

        try
        {
            return new Uri(configurationValue);
        }
        catch (Exception ex)
        {
            var errorMessage = $"OpAMP server URL configuration has an invalid value: '{configurationValue}'.";
            Logger.Error(ex, errorMessage);

            if (failFast)
            {
                throw new InvalidOperationException(errorMessage, ex);
            }

            return null;
        }
    }

    private static ConnectionType? GetConnectionType(string? configurationValue, bool failFast)
    {
        if (string.IsNullOrWhiteSpace(configurationValue))
        {
            // indicates that the default value should be used
            return null;
        }

        var parsedValue = configurationValue!.ToLower() switch
        {
            "websocket" => ConnectionType.WebSocket,
            "http" => ConnectionType.Http,
            _ => (ConnectionType?)null,
        };

        if (parsedValue == null)
        {
            var unsupportedMessage = $"OpAMP connection type configuration has an invalid value: '{configurationValue}'.";
            Logger.Error(unsupportedMessage);

            if (failFast)
            {
                throw new NotSupportedException(unsupportedMessage);
            }
        }

        return parsedValue;
    }
}
