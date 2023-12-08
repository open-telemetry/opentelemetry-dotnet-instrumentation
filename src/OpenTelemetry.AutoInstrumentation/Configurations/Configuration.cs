// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Represents the configuration taken from one or more configuration sources.
/// </summary>
internal class Configuration
{
    private readonly IConfigurationSource[] _sources;

    public Configuration(bool failFast, params IConfigurationSource[] sources)
    {
        _sources = sources;
        FailFast = failFast;
    }

    public bool FailFast { get; }

    /// <summary>
    /// Gets the <see cref="string"/> value of the first setting found with
    /// the specified key from the current list of configuration sources.
    /// Sources are queried in the order in which they were added.
    /// </summary>
    /// <param name="key">The key that identifies the setting.</param>
    /// <returns>The value of the setting, or <c>null</c> if not found.</returns>
    public string? GetString(string key)
    {
        return _sources.Select(source => source.GetString(key))
             .FirstOrDefault(value => !string.IsNullOrEmpty(value));
    }

    /// <summary>
    /// Gets the <see cref="int"/> value of the first setting found with
    /// the specified key from the current list of configuration sources.
    /// Sources are queried in the order in which they were added.
    /// </summary>
    /// <param name="key">The key that identifies the setting.</param>
    /// <returns>The value of the setting, or <c>null</c> if not found.</returns>
    public int? GetInt32(string key)
    {
        return _sources.Select(source => source.GetInt32(key))
            .FirstOrDefault(value => value.HasValue);
    }

    /// <summary>
    /// Gets the <see cref="double"/> value of the first setting found with
    /// the specified key from the current list of configuration sources.
    /// Sources are queried in the order in which they were added.
    /// </summary>
    /// <param name="key">The key that identifies the setting.</param>
    /// <returns>The value of the setting, or <c>null</c> if not found.</returns>
    public double? GetDouble(string key)
    {
        return _sources.Select(source => source.GetDouble(key))
            .FirstOrDefault(value => value.HasValue);
    }

    /// <summary>
    /// Gets the <see cref="bool"/> value of the first setting found with
    /// the specified key from the current list of configuration sources.
    /// Sources are queried in the order in which they were added.
    /// </summary>
    /// <param name="key">The key that identifies the setting.</param>
    /// <returns>The value of the setting, or <c>null</c> if not found.</returns>
    public bool? GetBool(string key)
    {
        return _sources.Select(source => source.GetBool(key))
            .FirstOrDefault(value => value.HasValue);
    }
}
