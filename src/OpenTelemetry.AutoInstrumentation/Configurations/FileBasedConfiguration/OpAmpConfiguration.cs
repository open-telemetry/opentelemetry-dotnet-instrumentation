// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class OpAmpConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the OpAmp client is enabled.
    /// </summary>
    [YamlMember(Alias = "enabled")]
    public bool? Enabled { get; set; }

    /// <summary>
    /// Gets or sets the URL of the server to which the application connects.
    /// </summary>
    [YamlMember(Alias = "server_url")]
    public string? ServerUrl { get; set; }

    /// <summary>
    /// Gets or sets the type of connection used for communication.
    /// </summary>
    [YamlMember(Alias = "connection_type")]
    public string? ConnectionType { get; set; }
}
