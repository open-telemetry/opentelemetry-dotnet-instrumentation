// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

[EmptyObjectOnEmptyYaml]
internal class RabbitMqConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the RabbitMQ instrumentation can capture the vhost and cluster name as span attributes.
    /// </summary>
    [YamlMember(Alias = "capture_vhost_and_cluster_name")]
    public bool CaptureVhostAndClusterName { get; set; }
}
