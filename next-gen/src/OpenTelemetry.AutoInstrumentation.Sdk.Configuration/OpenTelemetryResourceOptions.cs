// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;

namespace OpenTelemetry.Configuration;

/// <summary>
/// Contains OpenTelemetry resource options.
/// </summary>
public sealed class OpenTelemetryResourceOptions
{
    internal static OpenTelemetryResourceOptions ParseFromConfig(IConfigurationSection config)
    {
        Debug.Assert(config != null);

        List<OpenTelemetryResourceAttributeOptions> options = new();

        foreach (KeyValuePair<string, string?> attribute in config.AsEnumerable(makePathsRelative: true))
        {
            if (string.IsNullOrEmpty(attribute.Key)
                || string.IsNullOrEmpty(attribute.Value))
            {
                continue;
            }

            options.Add(new(attribute.Key, attribute.Value));
        }

        return new(options);
    }

    internal OpenTelemetryResourceOptions(
        IReadOnlyCollection<OpenTelemetryResourceAttributeOptions> attributeOptions)
    {
        Debug.Assert(attributeOptions != null);

        AttributeOptions = attributeOptions;
    }

    /// <summary>
    /// Gets the resource attribute options.
    /// </summary>
    public IReadOnlyCollection<OpenTelemetryResourceAttributeOptions> AttributeOptions { get; }
}
