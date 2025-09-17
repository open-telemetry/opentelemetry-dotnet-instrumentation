// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal class ResourceSettings : Settings
{
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
    public bool EnabledEnvironmentalVariablesDetector { get; set; } = true;

    protected override void OnLoadEnvVar(Configuration configuration)
    {
        var resourceDetectorsEnabledByDefault = configuration.GetBool(ConfigurationKeys.ResourceDetectorEnabled) ?? true;

        EnabledDetectors = configuration.ParseEnabledEnumList<ResourceDetector>(
            enabledByDefault: resourceDetectorsEnabledByDefault,
            enabledConfigurationTemplate: ConfigurationKeys.EnabledResourceDetectorTemplate);
    }

    protected override void OnLoadFile(YamlConfiguration configuration)
    {
        EnabledEnvironmentalVariablesDetector = false;

        var resourceAttributesWithPriority = configuration.Resource?.ParseAttributes() ?? [];

        var additionalResources = ParseResourceAttributes(configuration.Resource?.AttributesList);

        var merged = new Dictionary<string, object>();

        foreach (var kv in resourceAttributesWithPriority)
        {
            if (!merged.ContainsKey(kv.Key))
            {
                merged[kv.Key] = kv.Value;
            }
        }

        foreach (var kv in additionalResources)
        {
            if (!merged.ContainsKey(kv.Key))
            {
                merged[kv.Key] = kv.Value;
            }
        }

        Resources = merged.ToList();

        // TODO initialize EnabledDetectors from file configuration
    }

    private static List<KeyValuePair<string, object>> ParseResourceAttributes(string? resourceAttributes)
    {
        if (string.IsNullOrEmpty(resourceAttributes))
        {
            return [];
        }

        const char attributeListSplitter = ',';
        const char attributeKeyValueSplitter = '=';
        var attributes = new List<KeyValuePair<string, object>>();

        var rawAttributes = resourceAttributes!.Split(attributeListSplitter);
        foreach (var rawKeyValuePair in rawAttributes)
        {
            var keyValuePair = rawKeyValuePair.Split(attributeKeyValueSplitter);
            if (keyValuePair.Length != 2)
            {
                continue;
            }

            attributes.Add(new KeyValuePair<string, object>(keyValuePair[0].Trim(), keyValuePair[1].Trim()));
        }

        return attributes;
    }
}
