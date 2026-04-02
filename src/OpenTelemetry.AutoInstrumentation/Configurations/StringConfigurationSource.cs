// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// A base <see cref="IConfigurationSource"/> implementation
/// for string-only configuration sources.
/// </summary>
internal abstract class StringConfigurationSource : IConfigurationSource
{
    private readonly bool _failFast;

    protected StringConfigurationSource(bool failFast)
    {
        _failFast = failFast;
    }

    /// <inheritdoc />
    public abstract string? GetString(string key);

    /// <inheritdoc />
    public virtual int? GetInt32(string key)
    {
        var value = GetString(key);

        if (value == null)
        {
            return null;
        }

        if (_failFast)
        {
            return int.Parse(value, CultureInfo.InvariantCulture);
        }

        return int.TryParse(value, out var result)
            ? result
            : null;
    }

    /// <inheritdoc />
    public double? GetDouble(string key)
    {
        var value = GetString(key);

        if (value == null)
        {
            return null;
        }

        if (_failFast)
        {
            return double.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    public bool? GetBool(string key)
    {
        var value = GetString(key);

        if (value == null)
        {
            return null;
        }

        if (_failFast)
        {
            return bool.Parse(value);
        }

        return bool.TryParse(value, out var result)
            ? result
            : null;
    }
}
