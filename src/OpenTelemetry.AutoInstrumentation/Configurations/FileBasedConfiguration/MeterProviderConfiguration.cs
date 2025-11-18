// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class MeterProviderConfiguration
{
    [YamlMember(Alias = "readers")]
    public List<MetricReaderConfig> Readers { get; set; } = new();
}
