// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class OpAmpConfiguration
{
    /// <summary>
    /// Gets or sets the URL of the server to which the application connects.
    /// </summary>
    [YamlMember(Alias = "server_url")]
    public string? ServerUrl { get; set; }
}
