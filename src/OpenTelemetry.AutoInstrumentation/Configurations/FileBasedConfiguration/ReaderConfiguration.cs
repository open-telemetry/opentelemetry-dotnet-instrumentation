// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class ReaderConfiguration
{
    /// <summary>
    /// Gets or sets the exporter configuration for the reader.
    /// </summary>
    [YamlMember(Alias = "exporter")]
    public ExporterConfig Exporter { get; set; } = new();
}
