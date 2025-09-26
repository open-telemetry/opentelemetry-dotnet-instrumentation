// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class NoCodeConfiguration
{
    [YamlMember(Alias = "targets")]
    public NoCodeEntry[]? Targets { get; set; }
}
