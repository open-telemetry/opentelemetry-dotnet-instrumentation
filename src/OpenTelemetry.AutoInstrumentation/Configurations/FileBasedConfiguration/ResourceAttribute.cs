// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class ResourceAttribute
{
    /// <summary>
    /// Gets or sets the name of the resource attribute.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the value of the resource attribute.
    /// </summary>
    [YamlMember(Alias = "value")]
    public object Value { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of the resource attribute.
    /// </summary>
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "string";
}
