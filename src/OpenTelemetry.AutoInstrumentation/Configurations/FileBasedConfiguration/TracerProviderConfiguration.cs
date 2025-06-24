// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class TracerProviderConfiguration
{
    [YamlMember(Alias = "processors")]
    public Dictionary<string, BatchProcessorConfig> Processors { get; set; } = new();
}
