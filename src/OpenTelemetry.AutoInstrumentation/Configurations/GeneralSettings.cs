// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal class GeneralSettings : Settings
{
    /// <summary>
    /// Gets the list of plugins represented by <see cref="Type.AssemblyQualifiedName"/>.
    /// </summary>
    public IList<string> Plugins { get; } = new List<string>();

    /// <summary>
    /// Gets the list of enabled resource detectors.
    /// </summary>
    public IReadOnlyList<ResourceDetector> EnabledResourceDetectors { get; private set; } = new List<ResourceDetector>();

    /// <summary>
    /// Gets the list of enabled resources.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, object>> Resources { get; private set; } = new List<KeyValuePair<string, object>>();

    /// <summary>
    /// Gets a value indicating whether the <see cref="AppDomain.UnhandledException"/> event should trigger
    /// the flushing of telemetry data.
    /// Default is <c>false</c>.
    /// </summary>
    public bool FlushOnUnhandledException { get; private set; }

    /// <summary>
    /// Gets a value indicating whether OpenTelemetry .NET SDK should be set up.
    /// </summary>
    public bool SetupSdk { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the profiler is enabled.
    /// </summary>
    public bool ProfilerEnabled { get; private set; }

    protected override void OnLoadEnvVar(Configuration configuration)
    {
        var providerPlugins = configuration.GetString(ConfigurationKeys.ProviderPlugins);
        if (providerPlugins != null)
        {
            foreach (var pluginAssemblyQualifiedName in providerPlugins.Split(Constants.ConfigurationValues.DotNetQualifiedNameSeparator))
            {
                Plugins.Add(pluginAssemblyQualifiedName);
            }
        }

        var baseResources = new List<KeyValuePair<string, object>>(1);

        var serviceName = configuration.GetString(ConfigurationKeys.ServiceName);

        if (!string.IsNullOrEmpty(serviceName))
        {
            baseResources.Add(new KeyValuePair<string, object>(Constants.ResourceAttributes.AttributeServiceName, serviceName!));
        }

        var resourceAttributes = ParseResourceAttributes(configuration.GetString(ConfigurationKeys.ResourceAttributes));
        if (resourceAttributes.Count > 0)
        {
            foreach (var attr in resourceAttributes)
            {
                if (attr.Key == Constants.ResourceAttributes.AttributeServiceName && !string.IsNullOrEmpty(serviceName))
                {
                    continue; // OTEL_SERVICE_NAME takes precedence
                }

                baseResources.Add(attr);
            }
        }

        Resources = baseResources;

        var resourceDetectorsEnabledByDefault = configuration.GetBool(ConfigurationKeys.ResourceDetectorEnabled) ?? true;

        EnabledResourceDetectors = configuration.ParseEnabledEnumList<ResourceDetector>(
            enabledByDefault: resourceDetectorsEnabledByDefault,
            enabledConfigurationTemplate: ConfigurationKeys.EnabledResourceDetectorTemplate);

        FlushOnUnhandledException = configuration.GetBool(ConfigurationKeys.FlushOnUnhandledException) ?? false;
        SetupSdk = configuration.GetBool(ConfigurationKeys.SetupSdk) ?? true;

        ProfilerEnabled = configuration.GetString(ConfigurationKeys.ProfilingEnabled) == "1";
    }

    protected override void OnLoadFile(Conf configuration)
    {
        var resourceAttributesWithPriority = configuration.Resource?.ParseAttributes() ?? [];

        var resourceAttributes = ParseResourceAttributes(configuration.Resource?.AttributesList);

        var merged = new Dictionary<string, object>();

        foreach (var kv in resourceAttributesWithPriority)
        {
            if (!merged.ContainsKey(kv.Key))
            {
                merged[kv.Key] = kv.Value;
            }
        }

        foreach (var kv in resourceAttributes)
        {
            if (!merged.ContainsKey(kv.Key))
            {
                merged[kv.Key] = kv.Value;
            }
        }

        Resources = merged.ToList();
    }

    private static List<KeyValuePair<string, object>> ParseResourceAttributes(string? resourceAttributes)
    {
        if (string.IsNullOrEmpty(resourceAttributes))
        {
            return [];
        }

        var attributeListSplitter = ',';
        var attributeKeyValueSplitter = '=';
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
