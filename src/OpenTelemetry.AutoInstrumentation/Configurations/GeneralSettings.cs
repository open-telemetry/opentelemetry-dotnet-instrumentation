// <copyright file="GeneralSettings.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

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

    protected override void OnLoad(Configuration configuration)
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
}
