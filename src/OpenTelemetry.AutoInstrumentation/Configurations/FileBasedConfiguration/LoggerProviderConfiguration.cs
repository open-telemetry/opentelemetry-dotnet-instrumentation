// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class LoggerProviderConfiguration
{
    [YamlMember(Alias = "processors")]
    public List<LogProcessorConfig> Processors { get; set; } = new();
}
