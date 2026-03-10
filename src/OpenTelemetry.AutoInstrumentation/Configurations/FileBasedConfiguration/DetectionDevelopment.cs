// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class DetectionDevelopment
{
    /// <summary>
    /// Gets or sets the configuration for resource detectors.
    /// If omitted or null, no resource detectors are enabled.
    /// </summary>
    [YamlMember(Alias = "detectors")]
    public DotNetDetectors? Detectors { get; set; }
}
