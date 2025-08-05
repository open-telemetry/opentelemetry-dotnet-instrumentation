// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Configuration;

/// <summary>
/// Contains OpenTelemetry resource attribute options.
/// </summary>
public sealed class OpenTelemetryResourceAttributeOptions
{
    internal OpenTelemetryResourceAttributeOptions(
        string key,
        string valueOrExpression)
    {
        Debug.Assert(!string.IsNullOrEmpty(key));
        Debug.Assert(!string.IsNullOrEmpty(valueOrExpression));

        Key = key;
        ValueOrExpression = valueOrExpression;
    }

    /// <summary>
    /// Gets the resource attribute key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the resource attribute value or expression.
    /// </summary>
    public string ValueOrExpression { get; }
}
