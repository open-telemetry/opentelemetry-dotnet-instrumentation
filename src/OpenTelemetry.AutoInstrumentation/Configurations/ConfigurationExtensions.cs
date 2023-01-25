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

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal static class ConfigurationExtensions
{
    public static IList<TEnum> ParseEnabledEnumList<TEnum>(this Configuration source, string enabledConfiguration, string disabledConfiguration, string error)
        where TEnum : struct, Enum, IConvertible
    {
        var configurations = new Dictionary<string, TEnum>();
        var enabledConfigurations = source.GetString(enabledConfiguration);
        if (enabledConfigurations != null)
        {
            if (enabledConfigurations == Constants.ConfigurationValues.None)
            {
                return Array.Empty<TEnum>();
            }

            foreach (var configuration in enabledConfigurations.Split(Constants.ConfigurationValues.Separator))
            {
                if (Enum.TryParse(configuration, out TEnum parsedType))
                {
                    configurations[configuration] = parsedType;
                }
                else
                {
                    throw new FormatException(string.Format(error, configuration));
                }
            }
        }
        else
        {
            configurations = Enum.GetValues(typeof(TEnum))
                .Cast<TEnum>()
                .ToDictionary(
                    key => Enum.GetName(typeof(TEnum), key)!,
                    val => val);
        }

        var disabledConfigurations = source.GetString(disabledConfiguration);
        if (disabledConfigurations != null)
        {
            foreach (var configuration in disabledConfigurations.Split(Constants.ConfigurationValues.Separator))
            {
                if (Enum.TryParse(configuration, out TEnum _))
                {
                    configurations.Remove(configuration);
                }
                else
                {
                    throw new FormatException(string.Format(error, configuration));
                }
            }
        }

        return configurations.Values.ToList();
    }
}
