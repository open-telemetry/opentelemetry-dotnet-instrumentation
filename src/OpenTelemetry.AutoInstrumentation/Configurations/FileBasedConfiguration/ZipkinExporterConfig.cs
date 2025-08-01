// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class ZipkinExporterConfig
{
    public ZipkinExporterConfig()
    {
    }

    public ZipkinExporterConfig(string? endpoint)
    {
        if (endpoint is not null)
        {
            Endpoint = endpoint;
        }
    }

    /// <summary>
    /// Gets or sets the Zipkin endpoint URL to which spans are exported.
    /// If omitted or null, the default value "http://localhost:9411/api/v2/spans" is used.
    /// </summary>
    [YamlMember(Alias = "endpoint")]
    public string Endpoint { get; set; } = "http://localhost:9411/api/v2/spans";
}
