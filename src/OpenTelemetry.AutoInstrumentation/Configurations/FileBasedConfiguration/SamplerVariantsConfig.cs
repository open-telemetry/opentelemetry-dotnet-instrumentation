// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class SamplerVariantsConfig
{
    [YamlMember(Alias = "always_on")]
    public object? AlwaysOn { get; set; }

    [YamlMember(Alias = "always_off")]
    public object? AlwaysOff { get; set; }

    [YamlMember(Alias = "trace_id_ratio")]
    public TraceIdRatioSamplerConfig? TraceIdRatio { get; set; }
}
