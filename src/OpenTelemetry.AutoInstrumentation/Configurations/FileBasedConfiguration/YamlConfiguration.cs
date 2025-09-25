// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class YamlConfiguration
{
    /// <summary>
    /// Gets or sets the file format version.
    /// The yaml format is documented at
    /// https://github.com/open-telemetry/opentelemetry-configuration/tree/main/schema
    /// </summary>
    [YamlMember(Alias = "file_format")]
    public string? FileFormat { get; set; }

    /// <summary>
    /// Gets or sets the resource configuration.
    /// Configure resource for all signals.
    /// If omitted, the default resource is used.
    /// </summary>
    [YamlMember(Alias = "resource")]
    public ResourceConfiguration? Resource { get; set; }
}
