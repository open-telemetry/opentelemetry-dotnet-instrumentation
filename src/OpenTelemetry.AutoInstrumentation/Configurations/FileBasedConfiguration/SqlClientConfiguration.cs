// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

[EmptyObjectOnEmptyYaml]
internal class SqlClientConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the SqlClient instrumentation on .NET Framework should rewrite IL
    /// to ensure CommandText is available.
    /// </summary>
    [YamlMember(Alias = "netfx_ilrewrite_enabled")]
    public bool? NetFxIlRewriteEnabled { get; set; }
}
