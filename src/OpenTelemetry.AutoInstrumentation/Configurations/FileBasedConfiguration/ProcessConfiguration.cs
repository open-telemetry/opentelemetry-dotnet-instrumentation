// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

[EmptyObjectOnEmptyYaml]
internal class ProcessConfiguration
{
    /// <summary>
    /// Gets or sets the process environment context extraction configuration.
    /// </summary>
    [YamlMember(Alias = "context_extraction")]
    public ProcessContextExtractionConfiguration? ContextExtraction { get; set; }

    /// <summary>
    /// Gets or sets the child process context injection configuration.
    /// </summary>
    [YamlMember(Alias = "context_injection")]
    public ProcessContextInjectionConfiguration? ContextInjection { get; set; }
}
