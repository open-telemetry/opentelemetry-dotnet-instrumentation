// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

[EmptyObjectOnEmptyYaml]
internal class CaptureMetadataConfiguration
{
    /// <summary>
    /// Gets or sets a comma-separated list of gRPC metadata names.
    /// Grpc.Net.Client instrumentations will capture gRPC request metadata values for all configured metadata names.
    /// </summary>
    [YamlMember(Alias = "capture_request_metadata")]
    public string? CaptureRequestMetadata { get; set; }

    /// <summary>
    /// Gets or sets a comma-separated list of gRPC metadata names.
    /// Grpc.Net.Client instrumentations will capture gRPC response metadata values for all configured metadata names.
    /// </summary>
    [YamlMember(Alias = "capture_response_metadata")]
    public string? CaptureResponseMetadata { get; set; }
}
