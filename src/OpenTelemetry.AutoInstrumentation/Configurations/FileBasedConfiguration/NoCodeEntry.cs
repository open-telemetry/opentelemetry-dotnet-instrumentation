// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class NoCodeEntry
{
    [YamlMember(Alias = "target")]
    public NoCodeTarget? Target { get; set; }

    [YamlMember(Alias = "span")]
    public NoCodeSpan? Span { get; set; }
}
