// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class MetricPullExporterConfig
{
    /// <summary>
    /// Gets or sets the Prometheus exporter configuration.
    /// </summary>
    [YamlMember(Alias = "prometheus")]
    public object? Prometheus { get; set; }
}
