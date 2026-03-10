// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class PullMetricReaderConfig
{
    /// <summary>
    /// Gets or sets the exporter configuration.
    /// </summary>
    [YamlMember(Alias = "exporter")]
    public MetricPullExporterConfig? Exporter { get; set; }
}
