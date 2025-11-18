// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class NoCodeTarget
{
    [YamlMember(Alias = "assembly")]
    public NoCodeAssembly? Assembly { get; set; }

    [YamlMember(Alias = "type")]
    public string? Type { get; set; }

    [YamlMember(Alias = "method")]
    public string? Method { get; set; }

    [YamlMember(Alias = "signature")]
    public NoCodeSignature? Signature { get; set; }
}
