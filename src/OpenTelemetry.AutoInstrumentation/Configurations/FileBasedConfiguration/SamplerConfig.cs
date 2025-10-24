// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

[EmptyObjectOnEmptyYaml]
internal class SamplerConfig
{
    [YamlMember(Alias = "always_on")]
    public AlwaysOnSamplerConfig? AlwaysOn { get; set; }

    [YamlMember(Alias = "always_off")]
    public AlwaysOffSamplerConfig? AlwaysOff { get; set; }

    [YamlMember(Alias = "trace_id_ratio")]
    public TraceIdRatioSamplerConfig? TraceIdRatio { get; set; }

    [YamlMember(Alias = "traceidratio")]
    public TraceIdRatioSamplerConfig? TraceIdRatioLegacy { get; set; }

    [YamlMember(Alias = "parent_based")]
    public ParentBasedSamplerConfig? ParentBased { get; set; }
}
