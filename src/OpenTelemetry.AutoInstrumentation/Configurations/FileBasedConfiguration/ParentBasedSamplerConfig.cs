// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

[EmptyObjectOnEmptyYaml]
internal class ParentBasedSamplerConfig
{
    [YamlMember(Alias = "root")]
    public SamplerVariantsConfig? Root { get; set; }

    [YamlMember(Alias = "remote_parent_sampled")]
    public SamplerVariantsConfig? RemoteParentSampled { get; set; }

    [YamlMember(Alias = "remote_parent_not_sampled")]
    public SamplerVariantsConfig? RemoteParentNotSampled { get; set; }

    [YamlMember(Alias = "local_parent_sampled")]
    public SamplerVariantsConfig? LocalParentSampled { get; set; }

    [YamlMember(Alias = "local_parent_not_sampled")]
    public SamplerVariantsConfig? LocalParentNotSampled { get; set; }
}
