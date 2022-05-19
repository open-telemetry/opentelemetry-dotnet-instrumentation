// <copyright file="StringConfigurationSource.cs" company="OpenTelemetry Authors">
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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;

namespace OpenTelemetry.AutoInstrumentation.Configuration;

/// <summary>
/// A base <see cref="IConfigurationSource"/> implementation
/// for string-only configuration sources.
/// </summary>
public abstract class StringConfigurationSource : IConfigurationSource
{
    /// <inheritdoc />
    public abstract string GetString(string key);

    /// <inheritdoc />
    public virtual int? GetInt32(string key)
    {
        string value = GetString(key);

        return int.TryParse(value, out int result)
            ? result
            : (int?)null;
    }

    /// <inheritdoc />
    public double? GetDouble(string key)
    {
        string value = GetString(key);

        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result)
            ? result
            : (double?)null;
    }

    /// <inheritdoc />
    public virtual bool? GetBool(string key)
    {
        var value = GetString(key);
        return bool.TryParse(value, out bool result) ? result : null;
    }
}
