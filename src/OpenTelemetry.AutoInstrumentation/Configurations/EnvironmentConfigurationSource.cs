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
#pragma warning disable CA1031 // Do not catch general exception types.
        catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types.
        {
            // We should not add a dependency from the Configuration system to the Logger system,
            // so do nothing
        }

        return null;
    }
}
