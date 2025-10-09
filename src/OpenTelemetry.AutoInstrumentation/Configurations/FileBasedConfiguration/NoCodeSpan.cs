// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Globalization;
using OpenTelemetry.AutoInstrumentation.Logging;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class NoCodeSpan
{
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    [YamlMember(Alias = "kind")]
    public string? Kind { get; set; }

    [YamlMember(Alias = "attributes")]
    public List<ResourceAttribute>? Attributes { get; set; }

    public TagList ParseAttributes()
    {
        TagList tagList = default;

        if (Attributes == null || Attributes.Count == 0)
        {
            return tagList;
        }

        foreach (var attribute in Attributes)
        {
            if (ResourceAttribute.TryParseAttribute(attribute, out var name, out var value) && value != null)
            {
                tagList.Add(name, value);
            }
        }

        return tagList;
    }
}
