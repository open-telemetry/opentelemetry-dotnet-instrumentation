// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using OpenTelemetry.AutoInstrumentation.Logging;
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
        var resourceAttributesWithPriority = new Dictionary<string, object>();

        if (Attributes != null)
        {
            foreach (var attribute in Attributes)
            {
                if (YamlAttribute.TryParseAttribute(attribute, out var name, out var value) && value != null)
                {
                    resourceAttributesWithPriority[name] = value;
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
