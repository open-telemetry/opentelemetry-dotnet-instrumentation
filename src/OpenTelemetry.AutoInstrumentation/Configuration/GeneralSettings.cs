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

namespace OpenTelemetry.AutoInstrumentation.Configuration;

internal class GeneralSettings : Settings
{
    public GeneralSettings(IConfigurationSource source)
        : base(source)
    {
        var providerPlugins = source.GetString(ConfigurationKeys.ProviderPlugins);
        if (providerPlugins != null)
        {
            foreach (var pluginAssemblyQualifiedName in providerPlugins.Split(Constants.ConfigurationValues.DotNetQualifiedNameSeparator))
            {
                Plugins.Add(pluginAssemblyQualifiedName);
            }
        }

        FlushOnUnhandledException = source.GetBool(ConfigurationKeys.FlushOnUnhandledException) ?? false;
    }

    /// <summary>
    /// Gets the list of plugins represented by <see cref="Type.AssemblyQualifiedName"/>.
    /// </summary>
    public IList<string> Plugins { get; } = new List<string>();

    /// <summary>
    /// Gets a value indicating whether the <see cref="AppDomain.UnhandledException"/> event should trigger
    /// the flushing of telemetry data.
    /// Default is <c>false</c>.
    /// </summary>
    public bool FlushOnUnhandledException { get; }
}
