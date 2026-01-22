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

#pragma warning disable CS0618 // Type or member is obsolete. Zipkin is deprecated. It should be removed in December 2026.
    public void CopyTo(ZipkinExporterOptions options)
#pragma warning restore CS0618 // Type or member is obsolete. Zipkin is deprecated. It should be removed in December 2026.
    {
        options.Endpoint = new Uri(Endpoint);
    }
}
