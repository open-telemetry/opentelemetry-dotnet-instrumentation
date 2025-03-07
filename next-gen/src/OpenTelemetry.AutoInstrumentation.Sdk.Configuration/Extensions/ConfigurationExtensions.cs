// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.Configuration;
#pragma warning restore IDE0130 // Namespace does not match folder structure

internal static class ConfigurationExtensions
{
    public static bool TryParseValue(
        this IConfigurationSection config,
        string key,
        out int value)
    {
        Debug.Assert(!string.IsNullOrEmpty(key));
        Debug.Assert(config != null);

        string? valueString = config[key];
        if (!string.IsNullOrEmpty(valueString)
            && int.TryParse(valueString, out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    public static bool GetValueOrUseDefault(
        this IConfigurationSection config,
        string key,
        bool defaultValue)
    {
        Debug.Assert(!string.IsNullOrEmpty(key));
        Debug.Assert(config != null);

        string? valueString = config[key];
        if (!string.IsNullOrEmpty(valueString)
            && bool.TryParse(valueString, out bool value))
        {
            return value;
        }

        return defaultValue;
    }

    public static double GetValueOrUseDefault(
        this IConfigurationSection config,
        string key,
        double defaultValue)
    {
        Debug.Assert(!string.IsNullOrEmpty(key));
        Debug.Assert(config != null);

        string? valueString = config[key];
        if (!string.IsNullOrEmpty(valueString)
            && double.TryParse(valueString, out double value))
        {
            return value;
        }

        return defaultValue;
    }
}
