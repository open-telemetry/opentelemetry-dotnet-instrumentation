// <copyright file="Configuration.cs" company="OpenTelemetry Authors">
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

/// <summary>
/// Represents the configration taken from one or more configuration sources.
/// </summary>
internal class Configuration
{
    private readonly IConfigurationSource[] _sources;

    public Configuration(params IConfigurationSource[] sources)
    {
        _sources = sources;
    }

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
