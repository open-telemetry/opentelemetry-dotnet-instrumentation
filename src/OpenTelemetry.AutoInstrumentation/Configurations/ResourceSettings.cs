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
    public bool EnvironmentalVariablesDetectorEnabled { get; set; } = true;

    protected override void OnLoadEnvVar(Configuration configuration)
    {
        var resourceDetectorsEnabledByDefault = configuration.GetBool(ConfigurationKeys.ResourceDetectorEnabled) ?? true;

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
