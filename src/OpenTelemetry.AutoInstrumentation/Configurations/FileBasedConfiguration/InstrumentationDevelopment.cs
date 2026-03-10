// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class InstrumentationDevelopment
{
    /// <summary>
    /// Gets or sets the configuration for .NET language-specific instrumentation libraries.
    /// </summary>
    [YamlMember(Alias = "dotnet")]
    public DotNetInstrumentation? DotNet { get; set; }
}
