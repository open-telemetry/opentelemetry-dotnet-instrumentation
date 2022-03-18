// <copyright file="CompositeConfigurationSource.cs" company="OpenTelemetry Authors">
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OpenTelemetry.AutoInstrumentation.Configuration;

/// <summary>
/// Represents one or more configuration sources.
/// </summary>
public class CompositeConfigurationSource : IConfigurationSource, IEnumerable<IConfigurationSource>
{
    private readonly List<IConfigurationSource> _sources = new List<IConfigurationSource>();

    /// <summary>
    /// Adds a new configuration source to this instance.
    /// </summary>
    /// <param name="source">The configuration source to add.</param>
    public void Add(IConfigurationSource source)
    {
        if (source == null) { throw new ArgumentNullException(nameof(source)); }

        _sources.Add(source);
    }

    /// <summary>
    /// Inserts an element into the <see cref="CompositeConfigurationSource"/> at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
    /// <param name="item">The configuration source to insert.</param>
    public void Insert(int index, IConfigurationSource item)
    {
        if (item == null) { throw new ArgumentNullException(nameof(item)); }

        _sources.Insert(index, item);
    }

    /// <summary>
    /// Gets the <see cref="string"/> value of the first setting found with
    /// the specified key from the current list of configuration sources.
    /// Sources are queried in the order in which they were added.
    /// </summary>
    /// <param name="key">The key that identifies the setting.</param>
    /// <returns>The value of the setting, or <c>null</c> if not found.</returns>
    public string GetString(string key)
    {
        return _sources.Select(source => source.GetString(key))
            .FirstOrDefault(value => value != null);
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
            .FirstOrDefault(value => value != null);
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
            .FirstOrDefault(value => value != null);
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
            .FirstOrDefault(value => value != null);
    }

    /// <inheritdoc />
    IEnumerator<IConfigurationSource> IEnumerable<IConfigurationSource>.GetEnumerator()
    {
        return _sources.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return _sources.GetEnumerator();
    }

    /// <inheritdoc />
    public IDictionary<string, string> GetDictionary(string key)
    {
        return _sources.Select(source => source.GetDictionary(key))
            .FirstOrDefault(value => value != null);
    }

    /// <inheritdoc />
    public IDictionary<string, string> GetDictionary(string key, bool allowOptionalMappings)
    {
        return _sources.Select(source => source.GetDictionary(key, allowOptionalMappings))
            .FirstOrDefault(value => value != null);
    }
}
