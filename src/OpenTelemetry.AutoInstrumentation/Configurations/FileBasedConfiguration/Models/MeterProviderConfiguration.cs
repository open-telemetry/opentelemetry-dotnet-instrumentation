// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Models;

internal class MeterProviderConfiguration
{
    /// <summary>
    /// Gets or sets the readers for the meter provider.
    /// </summary>
    [YamlMember(Alias = "readers")]
    public Dictionary<string, ReaderConfiguration> Readers { get; set; } = new();
}
