// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class NoCodeSignature
{
    [YamlMember(Alias = "return_type")]
    public string? ReturnType { get; set; }

    [YamlMember(Alias = "parameter_types")]
    public string[]? ParameterTypes { get; set; }
}
