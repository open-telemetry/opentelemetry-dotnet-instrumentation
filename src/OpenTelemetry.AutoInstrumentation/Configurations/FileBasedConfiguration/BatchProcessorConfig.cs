// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class BatchProcessorConfig : BatchProcessorConfigBase
{
    /// <summary>
    /// Gets or sets the exporters.
    /// </summary>
    [YamlMember(Alias = "exporter")]
    public BatchTracerExporterConfig? Exporter { get; set; }
}
