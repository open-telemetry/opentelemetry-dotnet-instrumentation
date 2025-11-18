// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

[EmptyObjectOnEmptyYaml]
internal class CaptureHeadersConfiguration
{
    /// <summary>
    /// Gets or sets a comma-separated list of HTTP header names.
    /// Instrumentations will capture HTTP request header values for all configured header names.
    /// </summary>
    [YamlMember(Alias = "capture_request_headers")]
    public string? CaptureRequestHeaders { get; set; }

    /// <summary>
    /// Gets or sets a comma-separated list of HTTP header names.
    /// Instrumentations will capture HTTP response header values for all configured header names.
    /// </summary>
    [YamlMember(Alias = "capture_response_headers")]
    public string? CaptureResponseHeaders { get; set; }
}
