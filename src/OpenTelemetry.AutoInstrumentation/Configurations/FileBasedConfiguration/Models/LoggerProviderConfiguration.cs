// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Models;

internal class LoggerProviderConfiguration
{
    /// <summary>
    /// Gets or sets the processors for the logger provider.
    /// </summary>
    [YamlMember(Alias = "processors")]
    public Dictionary<string, BatchProcessorConfig> Processors { get; set; } = new();
}
