// <copyright file="ConfigurationExtensions.cs" company="OpenTelemetry Authors">
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

using System.Globalization;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal static class ConfigurationExtensions
{
    public static IList<TEnum> ParseEnabledEnumList<TEnum>(this Configuration source, bool enabledByDefault, string enabledConfigurationTemplate)
        where TEnum : struct, Enum, IConvertible
    {
        var allConfigurations = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToArray();
        var enabledConfigurations = new List<TEnum>(allConfigurations.Length);

        foreach (var configuration in allConfigurations)
        {
            var configurationEnabled = source.GetBool(string.Format(CultureInfo.InvariantCulture, enabledConfigurationTemplate, configuration.ToString().ToUpperInvariant())) ?? enabledByDefault;

            if (configurationEnabled)
            {
                enabledConfigurations.Add(configuration);
            }
        }

        return enabledConfigurations;
    }
}
