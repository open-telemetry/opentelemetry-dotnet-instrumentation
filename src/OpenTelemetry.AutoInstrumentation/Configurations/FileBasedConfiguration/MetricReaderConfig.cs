// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class MetricReaderConfig
{
    [YamlMember(Alias = "periodic")]
    public PeriodicMetricReaderConfig? Periodic { get; set; }

    [YamlMember(Alias = "pull")]
    public PullMetricReaderConfig? Pull { get; set; }
}
