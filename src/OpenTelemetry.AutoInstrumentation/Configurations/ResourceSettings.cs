// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal class ResourceSettings : Settings
{
    private const char AttributeListSplitter = ',';
    private static readonly char[] AttributeKeyValueSplitter = ['='];

    /// <summary>
    /// Gets or sets the list of enabled resource detectors.
    /// </summary>
    public IReadOnlyList<ResourceDetector> EnabledDetectors { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of enabled resources.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, object>> Resources { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether environmental variables resource detector is enabled.
    /// </summary>
    public bool EnvironmentalVariablesDetectorEnabled { get; set; } = true;

    // from https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.15.0/src/OpenTelemetry/Resources/OtelEnvResourceDetector.cs#L35
    internal static List<KeyValuePair<string, object>> ParseResourceAttributes(Configuration configuration)
    {
        var rawResourceAttributes = configuration.GetString(ConfigurationKeys.ResourceAttributes);
        var resourceAttributes = new List<KeyValuePair<string, object>>();

        if (rawResourceAttributes != null && !string.IsNullOrWhiteSpace(rawResourceAttributes))
        {
            var rawAttributes = rawResourceAttributes.Split(AttributeListSplitter);
            foreach (var rawKeyValuePair in rawAttributes)
            {
                var keyValuePair = rawKeyValuePair.Split(AttributeKeyValueSplitter, 2);
                if (keyValuePair.Length != 2)
                {
                    continue;
                }

                var value = WebUtility.UrlDecode(keyValuePair[1].Trim());
                resourceAttributes.Add(new(keyValuePair[0].Trim(), value));
            }
        }

        return resourceAttributes;
    }

    protected override void OnLoadEnvVar(Configuration configuration)
    {
        var resourceDetectorsEnabledByDefault = configuration.GetBool(ConfigurationKeys.ResourceDetectorEnabled) ?? true;
        var attributes = ParseResourceAttributes(configuration);

        var serviceName = configuration.GetString(ConfigurationKeys.ServiceName);
        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            attributes.RemoveAll(attribute => attribute.Key == Constants.ResourceAttributes.AttributeServiceName);
            attributes.Add(new(Constants.ResourceAttributes.AttributeServiceName, serviceName!));
        }

        Resources = attributes;
        EnabledDetectors = configuration.ParseEnabledEnumList<ResourceDetector>(
            enabledByDefault: resourceDetectorsEnabledByDefault,
            enabledConfigurationTemplate: ConfigurationKeys.EnabledResourceDetectorTemplate);
    }

    protected override void OnLoadFile(YamlConfiguration configuration)
    {
        EnvironmentalVariablesDetectorEnabled = false;

        Resources = configuration.Resource?.ParseAttributes() ?? [];

        EnabledDetectors = configuration.Resource?.DetectionDevelopment?.Detectors?.GetEnabledResourceDetectors() ?? [];
    }
}
