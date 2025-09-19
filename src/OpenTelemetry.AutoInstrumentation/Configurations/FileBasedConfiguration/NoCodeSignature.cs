// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class NoCodeSignature
{
    [YamlMember(Alias = "returned_type")]
    public string? ReturnedType { get; set; }

    [YamlMember(Alias = "parameter_types")]
    public string[]? ParameterTypes { get; set; }
}
