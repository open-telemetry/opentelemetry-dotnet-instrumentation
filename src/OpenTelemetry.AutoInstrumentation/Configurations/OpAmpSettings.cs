// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Util;

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

    protected override void OnLoadEnvVar(Configuration configuration)
    {
        OpAmpClientEnabled = configuration.GetBool(ConfigurationKeys.OpAmpEnabled) ?? false;
        ServerUrl = GetServerUrl(configuration.GetString(ConfigurationKeys.OpAmpServerUrl), configuration.FailFast);
    }

    protected override void OnLoadFile(YamlConfiguration configuration)
    {
        OpAmpClientEnabled = configuration.OpAmp != null;
        ServerUrl = GetServerUrl(configuration.OpAmp?.ServerUrl, configuration.FailFast);
    }

    private static Uri? GetServerUrl(string? configurationValue, bool failFast)
    {
        if (string.IsNullOrWhiteSpace(configurationValue))
        {
            // indicates that the default value should be used
            return null;
        }

        // Try build an absolute url.
        if (!Uri.TryCreate(configurationValue, UriKind.Absolute, out var serverUrl))
        {
            var errorMessage = $"OpAMP server URL configuration has an invalid value: '{configurationValue}'. The value must be an absolute URI.";
            Logger.Error(errorMessage);

            if (failFast)
            {
                throw new InvalidOperationException(errorMessage);
            }

            return null;
        }

        // Verify supported url schemes
        if (serverUrl.Scheme != UriSchemes.Http &&
            serverUrl.Scheme != UriSchemes.Https &&
            serverUrl.Scheme != UriSchemes.Ws &&
            serverUrl.Scheme != UriSchemes.Wss)
        {
            var errorMessage = $"OpAMP server URL configuration has an invalid value: '{configurationValue}'. Supported URI schemes are http, https, ws, and wss.";
            Logger.Error(errorMessage);

            if (failFast)
            {
                throw new InvalidOperationException(errorMessage);
            }

            return null;
        }

        return serverUrl;
    }
}
