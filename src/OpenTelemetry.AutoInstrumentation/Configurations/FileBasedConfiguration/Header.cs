// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class Header
{
    /// <summary>
    /// Gets or sets the name of the header.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the value of the header.
    /// </summary>
    [YamlMember(Alias = "value")]
    public string? Value { get; set; }
}
