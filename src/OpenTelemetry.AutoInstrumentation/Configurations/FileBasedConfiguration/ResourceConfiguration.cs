// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class ResourceConfiguration
{
    /// <summary>
    /// Gets or sets the list of resource attributes.
    /// </summary>
    [YamlMember(Alias = "attributes")]
    public List<YamlAttribute>? Attributes { get; set; }

    /// <summary>
    /// Gets or sets the attributes list for the resource.
    /// </summary>
    [YamlMember(Alias = "attributes_list")]
    public string? AttributesList { get; set; }

    /// <summary>
    /// Gets or sets the detection development configuration.
    /// </summary>
    [YamlMember(Alias = "detection/development")]
    public DetectionDevelopment? DetectionDevelopment { get; set; }

    public List<KeyValuePair<string, object>> ParseAttributes()
    {
        var resourceAttributesWithPriority = new List<KeyValuePair<string, object>>();

        if (Attributes != null)
        {
            resourceAttributesWithPriority = YamlAttribute.ParseAttributes(Attributes)
                .Where(kv => kv.Value != null)
                .Select(kv => new KeyValuePair<string, object>(kv.Key, kv.Value!))
                .ToList();
        }

        if (!string.IsNullOrEmpty(AttributesList))
        {
            const char attributeListSplitter = ',';
            char[] attributeKeyValueSplitter = ['='];

            var rawAttributes = AttributesList!.Split(attributeListSplitter);
            foreach (var rawKeyValuePair in rawAttributes)
            {
                var keyValuePair = rawKeyValuePair.Split(attributeKeyValueSplitter, 2);
                if (keyValuePair.Length != 2)
                {
                    continue;
                }

                var key = keyValuePair[0].Trim();
                var value = keyValuePair[1].Trim();

                if (!resourceAttributesWithPriority.Any(kvp => kvp.Key == key))
                {
                    resourceAttributesWithPriority.Add(new KeyValuePair<string, object>(key, value));
                }
            }
        }

        return resourceAttributesWithPriority;
    }
}
