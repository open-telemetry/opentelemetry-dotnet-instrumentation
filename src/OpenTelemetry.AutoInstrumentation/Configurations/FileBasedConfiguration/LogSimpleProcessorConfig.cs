// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class LogSimpleProcessorConfig
{
    [YamlMember(Alias = "exporter")]
    public SimpleLogExporterConfig? Exporter { get; set; }
}
