// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class OpAmpConfiguration
{
    /// <summary>
    /// Gets or sets the URL of the server to which the application connects.
    /// </summary>
    [YamlMember(Alias = "server_url")]
    public string? ServerUrl { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of custom messages that may wait to be sent.
    /// </summary>
    [YamlMember(Alias = "max_pending_custom_messages")]
    public int? MaxPendingCustomMessages { get; set; }

    /// <summary>
    /// Gets or sets the maximum aggregate size, in bytes, of pending custom message payloads.
    /// </summary>
    [YamlMember(Alias = "max_pending_custom_message_bytes")]
    public int? MaxPendingCustomMessageBytes { get; set; }
}
