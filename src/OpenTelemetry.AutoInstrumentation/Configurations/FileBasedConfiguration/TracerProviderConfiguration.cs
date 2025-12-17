// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class TracerProviderConfiguration
{
    [YamlMember(Alias = "processors")]
    public List<ProcessorConfig> Processors { get; set; } = [];

    [YamlMember(Alias = "sampler")]
    public SamplerConfig? Sampler { get; set; }
}
