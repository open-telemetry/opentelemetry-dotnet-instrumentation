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
        var result = new List<KeyValuePair<string, object>>();
        if (Attributes == null)
        {
            return result;
        }

        foreach (var attr in Attributes)
        {
            // TODO parse type and converting the value accordingly.

            result.Add(new KeyValuePair<string, object>(attr.Name, attr.Value));
        }

        return result;
    }
}
