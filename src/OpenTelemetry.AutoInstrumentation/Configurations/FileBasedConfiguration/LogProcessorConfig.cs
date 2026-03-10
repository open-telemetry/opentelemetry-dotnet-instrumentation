// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class LogProcessorConfig
{
    [YamlMember(Alias = "batch")]
    public LogBatchProcessorConfig? Batch { get; set; }

    [YamlMember(Alias = "simple")]
    public LogSimpleProcessorConfig? Simple { get; set; }
}
