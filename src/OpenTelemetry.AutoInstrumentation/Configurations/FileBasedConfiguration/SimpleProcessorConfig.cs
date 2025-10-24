// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class SimpleProcessorConfig
{
    [YamlMember(Alias = "exporter")]
    public SimpleTracerExporterConfig? Exporter { get; set; }
}
