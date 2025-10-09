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
    public List<YamlAttribute>? Attributes { get; set; }

    public TagList ParseAttributes()
    {
        if (Attributes == null || Attributes.Count == 0)
        {
            return default;
        }

        var list = YamlAttribute.ParseAttributes(Attributes);

        TagList tagList = default;
        foreach (var kvp in list)
        {
            tagList.Add(kvp.Key, kvp.Value);
        }

        return tagList;
    }
}
