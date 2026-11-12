// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

[EmptyObjectOnEmptyYaml]
internal class ProcessContextExtractionConfiguration
{
    /// <summary>
    /// Gets or sets how extracted context is applied. Defaults to parent.
    /// </summary>
    [YamlMember(Alias = "mode")]
    public string? Mode { get; set; }
}
