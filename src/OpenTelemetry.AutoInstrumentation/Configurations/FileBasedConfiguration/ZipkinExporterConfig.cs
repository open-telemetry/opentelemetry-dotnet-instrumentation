// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using OpenTelemetry.Exporter;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

[EmptyObjectOnEmptyYaml]
internal class ZipkinExporterConfig
{
    /// <summary>
    /// Gets or sets the Zipkin endpoint URL to which spans are exported.
    /// If omitted or null, the default value "http://localhost:9411/api/v2/spans" is used.
    /// </summary>
    [YamlMember(Alias = "endpoint")]
    public string Endpoint { get; set; } = "http://localhost:9411/api/v2/spans";

    public void CopyTo(ZipkinExporterOptions options)
    {
        options.Endpoint = new Uri(Endpoint);
    }
}
