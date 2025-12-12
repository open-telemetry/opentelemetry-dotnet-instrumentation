// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Represents a configuration source that
/// retrieves values from environment variables.
/// </summary>
internal class EnvironmentConfigurationSource : StringConfigurationSource
{
    public EnvironmentConfigurationSource(bool failFast)
        : base(failFast)
    {
    }

    public override string? GetString(string key)
    {
        try
        {
            return Environment.GetEnvironmentVariable(key);
        }
        catch (Exception)
        {
            // We should not add a dependency from the Configuration system to the Logger system,
            // so do nothing
        }

        return null;
    }
}
