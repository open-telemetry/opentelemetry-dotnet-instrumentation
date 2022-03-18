// <copyright file="IConfigurationSource.cs" company="OpenTelemetry Authors">
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

using System.Collections.Generic;

namespace OpenTelemetry.AutoInstrumentation.Configuration;

/// <summary>
/// A source of configuration settings, identifiable by a string key.
/// </summary>
public interface IConfigurationSource
{
    /// <summary>
    /// Gets the <see cref="string"/> value of
    /// the setting with the specified key.
    /// </summary>
    /// <param name="key">The key that identifies the setting.</param>
    /// <returns>The value of the setting, or <c>null</c> if not found.</returns>
    string GetString(string key);

    /// <summary>
    /// Gets the <see cref="int"/> value of
    /// the setting with the specified key.
    /// </summary>
    /// <param name="key">The key that identifies the setting.</param>
    /// <returns>The value of the setting, or <c>null</c> if not found.</returns>
    int? GetInt32(string key);

    /// <summary>
    /// Gets the <see cref="double"/> value of
    /// the setting with the specified key.
    /// </summary>
    /// <param name="key">The key that identifies the setting.</param>
    /// <returns>The value of the setting, or <c>null</c> if not found.</returns>
    double? GetDouble(string key);

    /// <summary>
    /// Gets the <see cref="bool"/> value of
    /// the setting with the specified key.
    /// </summary>
    /// <param name="key">The key that identifies the setting.</param>
    /// <returns>The value of the setting, or <c>null</c> if not found.</returns>
    bool? GetBool(string key);

    /// <summary>
    /// Gets the <see cref="IDictionary{TKey, TValue}"/> value of
    /// the setting with the specified key.
    /// </summary>
    /// <param name="key">The key that identifies the setting.</param>
    /// <returns>The value of the setting, or <c>null</c> if not found.</returns>
    IDictionary<string, string> GetDictionary(string key);

    /// <summary>
    /// Gets the <see cref="IDictionary{TKey, TValue}"/> value of
    /// the setting with the specified key.
    /// </summary>
    /// <param name="key">The key that identifies the setting.</param>
    /// <param name="allowOptionalMappings">Determines whether to create dictionary entries when the input has no value mapping</param>
    /// <returns>The value of the setting, or <c>null</c> if not found.</returns>
    IDictionary<string, string> GetDictionary(string key, bool allowOptionalMappings);
}
