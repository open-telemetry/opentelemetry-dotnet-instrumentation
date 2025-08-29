// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class AttributeLimits
{
    public AttributeLimits()
    {
    }

    public AttributeLimits(int? attributeValueLengthLimit, int? attributeCountLimit)
    {
        if (attributeValueLengthLimit.HasValue && attributeValueLengthLimit.Value >= 0)
        {
            AttributeValueLengthLimit = attributeValueLengthLimit;
        }

        if (attributeCountLimit.HasValue && attributeCountLimit.Value >= 0)
        {
            AttributeCountLimit = attributeCountLimit.Value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum attribute value size.
    /// Value must be non-negative.
    /// If omitted or null, there is no limit.
    /// </summary>
    [YamlMember(Alias = "attribute_value_length_limit")]
    public int? AttributeValueLengthLimit { get; set; }

    /// <summary>
    /// Gets or sets the maximum attribute count.
    /// Value must be non-negative.
    /// If omitted or null, 128 is used.
    /// </summary>
    [YamlMember(Alias = "attribute_count_limit")]
    public int AttributeCountLimit { get; set; } = 128;
}
