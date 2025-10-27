// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class PrometheusExporterConfig
{
    /// <summary>
    /// Gets or sets the host used by the Prometheus exporter.
    /// </summary>
    [YamlMember(Alias = "host")]
    public string? Host { get; set; }

    /// <summary>
    /// Gets or sets the port used by the Prometheus exporter.
    /// </summary>
    [YamlMember(Alias = "port")]
    public int? Port { get; set; }
}
