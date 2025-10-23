// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class ProcessorConfig
{
    [YamlMember(Alias = "batch")]
    public BatchProcessorConfig? Batch { get; set; }

    [YamlMember(Alias = "simple")]
    public SimpleProcessorConfig? Simple { get; set; }
}
