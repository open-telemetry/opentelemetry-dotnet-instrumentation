// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Models;

internal class PropagatorConfiguration
{
    /// <summary>
    /// Gets or sets the composite propagator configuration.
    /// </summary>
    [YamlMember(Alias = "composite")]
    public Dictionary<string, object>? Composite { get; set; }

    /// <summary>
    /// Gets or sets the composite list for the propagator.
    /// </summary>
    [YamlMember(Alias = "composite_list")]
    public string? CompositeList { get; set; }
}
