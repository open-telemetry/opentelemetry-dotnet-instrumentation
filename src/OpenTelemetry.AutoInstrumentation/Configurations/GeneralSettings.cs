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
        var detectors = configuration.Resource?.DetectionDevelopment?.Detectors;
        if (detectors != null)
        {
            var enabledDetectors = new List<ResourceDetector>();
#if NET
            if (detectors.Container != null)
            {
                enabledDetectors.Add(ResourceDetector.Container);
            }
#endif
            if (detectors.Process != null)
            {
                enabledDetectors.Add(ResourceDetector.Process);
            }

            if (detectors.AzureAppService != null)
            {
                enabledDetectors.Add(ResourceDetector.AzureAppService);
            }

            if (detectors.ProcessRuntime != null)
            {
                enabledDetectors.Add(ResourceDetector.ProcessRuntime);
            }

            if (detectors.OperatingSystem != null)
            {
                enabledDetectors.Add(ResourceDetector.OperatingSystem);
            }

            if (detectors.Host != null)
            {
                enabledDetectors.Add(ResourceDetector.Host);
            }

            EnabledResourceDetectors = enabledDetectors;
        }

        SetupSdk = configuration.Disabled;
    }
}
