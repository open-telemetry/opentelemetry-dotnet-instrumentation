// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

/// <summary>
/// Configuration for span status.
/// </summary>
internal class NoCodeStatusConfig
{
    [YamlMember(Alias = "rules")]
    public List<NoCodeStatusRuleConfig>? Rules { get; set; }
}
