// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;

// Note: Resource generates a warning with the Resource namespace from proto
#pragma warning disable CA1724 // Type names should not match namespaces

namespace OpenTelemetry.Resources;

/// <summary>
/// Contains information about the entity emitting telemetry.
/// </summary>
public sealed record Resource
{
    private static KeyValuePair<string, object> SanitizeAttribute(
        KeyValuePair<string, object> attribute)
    {
        if (attribute.Key == null)
        {
            throw new NotSupportedException("Resource attributes with null keys are not supported");
        }

        return !TrySanitizeValue(attribute.Value, out object? sanitizedValue)
            ? throw new NotSupportedException($"Resource attribute key '{attribute.Key}' value '{attribute.Value}' is not supported")
            : new(attribute.Key, sanitizedValue);
    }

    private static bool TrySanitizeValue(
        object value,
        [NotNullWhen(true)] out object? sanitizedValue)
    {
        sanitizedValue = value switch
        {
            string => value,
            bool => value,
            double => value,
            long => value,
            string[] => value,
            bool[] => value,
            double[] => value,
            long[] => value,
            int => Convert.ToInt64(value, CultureInfo.InvariantCulture),
            short => Convert.ToInt64(value, CultureInfo.InvariantCulture),
            float => Convert.ToDouble(value, CultureInfo.InvariantCulture),
            int[] v => Array.ConvertAll(v, Convert.ToInt64),
            short[] v => Array.ConvertAll(v, Convert.ToInt64),
            float[] v => Array.ConvertAll(v, f => Convert.ToDouble(f, CultureInfo.InvariantCulture)),
            _ => null,
        };

        return sanitizedValue != null;
    }

    private readonly KeyValuePair<string, object>[] _Attributes;

    /// <summary>
    /// Initializes a new instance of the <see cref="Resource"/> class.
    /// </summary>
    /// <param name="attributes">Resource attributes.</param>
    public Resource(
        IEnumerable<KeyValuePair<string, object>> attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);

        _Attributes = [.. attributes.Select(SanitizeAttribute)];
    }

    /// <summary>
    /// Gets the resource attributes.
    /// </summary>
    public ReadOnlySpan<KeyValuePair<string, object>> Attributes
        => _Attributes;
}
