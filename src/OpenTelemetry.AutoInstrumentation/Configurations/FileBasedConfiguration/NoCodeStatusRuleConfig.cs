// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

/// <summary>
/// Configuration for a single status rule.
/// </summary>
internal class NoCodeStatusRuleConfig
{
    /// <summary>
    /// Gets or sets the condition expression (e.g., "isnull($return)", "equals($return.Success, false)").
    /// </summary>
    [YamlMember(Alias = "condition")]
    public string? Condition { get; set; }

    /// <summary>
    /// Gets or sets the status code: "ok", "error", or "unset".
    /// </summary>
    [YamlMember(Alias = "code")]
    public string? Code { get; set; }

    /// <summary>
    /// Gets or sets the optional status description (often used with error status).
    /// </summary>
    [YamlMember(Alias = "description")]
    public string? Description { get; set; }
}
