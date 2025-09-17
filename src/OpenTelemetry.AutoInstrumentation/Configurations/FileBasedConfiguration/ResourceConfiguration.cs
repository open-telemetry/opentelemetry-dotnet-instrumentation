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
    public List<ResourceAttribute>? Attributes { get; set; }

    /// <summary>
    /// Gets or sets the attributes list for the resource.
    /// </summary>
    [YamlMember(Alias = "attributes_list")]
    public string? AttributesList { get; set; }

    public List<KeyValuePair<string, object>> ParseAttributes()
    {
        var resourceAttributesWithPriority = new Dictionary<string, object>();

        if (Attributes != null)
        {
            foreach (var attr in Attributes)
            {
                if (!resourceAttributesWithPriority.ContainsKey(attr.Name))
                {
                    // TODO parse type and converting the value accordingly.
                    resourceAttributesWithPriority.Add(attr.Name, attr.Value);
                }
            }
        }

        if (AttributesList != null)
        {
            const char attributeListSplitter = ',';
            char[] attributeKeyValueSplitter = ['='];

            var rawAttributes = AttributesList.Split(attributeListSplitter);
            foreach (var rawKeyValuePair in rawAttributes)
            {
                var keyValuePair = rawKeyValuePair.Split(attributeKeyValueSplitter, 2);
                if (keyValuePair.Length != 2)
                {
                    continue;
                }

                var key = keyValuePair[0].Trim();

                if (!resourceAttributesWithPriority.ContainsKey(key))
                {
                    resourceAttributesWithPriority.Add(key, keyValuePair[1].Trim());
                }
            }
        }

        return resourceAttributesWithPriority.ToList();
    }
}
