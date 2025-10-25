// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

[EmptyObjectOnEmptyYaml]
internal class ParentBasedSamplerConfig
{
    [YamlMember(Alias = "root")]
    public SamplerConfig? Root { get; set; }

    [YamlMember(Alias = "remote_parent_sampled")]
    public SamplerConfig? RemoteParentSampled { get; set; }

    [YamlMember(Alias = "remote_parent_not_sampled")]
    public SamplerConfig? RemoteParentNotSampled { get; set; }

    [YamlMember(Alias = "local_parent_sampled")]
    public SamplerConfig? LocalParentSampled { get; set; }

    [YamlMember(Alias = "local_parent_not_sampled")]
    public SamplerConfig? LocalParentNotSampled { get; set; }
}
