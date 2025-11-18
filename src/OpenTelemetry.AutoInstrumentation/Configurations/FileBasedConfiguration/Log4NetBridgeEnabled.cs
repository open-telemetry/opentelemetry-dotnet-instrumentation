// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

[EmptyObjectOnEmptyYaml]
internal class Log4NetBridgeEnabled
{
    /// <summary>
    /// Gets or sets a value indicating whether the Log4Net bridge is enabled.
    /// </summary>
    [YamlMember(Alias = "bridge_enabled")]
    public bool BridgeEnabled { get; set; }
}
