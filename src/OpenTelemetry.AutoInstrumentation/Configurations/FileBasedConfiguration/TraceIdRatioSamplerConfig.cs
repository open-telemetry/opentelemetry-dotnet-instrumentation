// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class TraceIdRatioSamplerConfig
{
    [YamlMember(Alias = "ratio")]
    public double? Ratio { get; set; }
}
