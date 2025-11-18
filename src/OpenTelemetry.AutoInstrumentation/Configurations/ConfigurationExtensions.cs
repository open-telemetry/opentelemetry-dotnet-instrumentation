// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal static class ConfigurationExtensions
{
    public static IReadOnlyList<TEnum> ParseEnabledEnumList<TEnum>(this Configuration source, bool enabledByDefault, string enabledConfigurationTemplate)
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

    public static IReadOnlyList<string> ParseList(this Configuration source, string key, char valueSeparator)
    {
        var values = source.GetString(key);

        if (string.IsNullOrWhiteSpace(values))
        {
            return [];
        }

        return values!.Split([valueSeparator], StringSplitOptions.RemoveEmptyEntries);
    }
}
