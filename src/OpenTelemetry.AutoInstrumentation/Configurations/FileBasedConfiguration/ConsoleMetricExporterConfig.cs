// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class ConsoleMetricExporterConfig
{
    /// <summary>
    /// Gets or sets the temporality preference for the console exporter.
    /// Values include: cumulative, delta, low_memory. If omitted or null, cumulative is used.
    /// </summary>
    [YamlMember(Alias = "temporality_preference")]
    public string? TemporalityPreference { get; set; }
}
