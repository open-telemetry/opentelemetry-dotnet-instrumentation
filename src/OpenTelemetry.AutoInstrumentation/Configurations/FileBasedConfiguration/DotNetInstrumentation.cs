// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class DotNetInstrumentation
{
    /// <summary>
    /// Gets or sets the configuration for .NET traces instrumentation.
    /// </summary>
    [YamlMember(Alias = "traces")]
    public DotNetTraces? Traces { get; set; }

    /// <summary>
    /// Gets or sets the configuration for .NET metrics instrumentation.
    /// </summary>
    [YamlMember(Alias = "metrics")]
    public DotNetMetrics? Metrics { get; set; }

    /// <summary>
    /// Gets or sets the configuration for .NET logs instrumentation.
    /// </summary>
    [YamlMember(Alias = "logs")]
    public DotNetLogs? Logs { get; set; }

    /// <summary>
    /// Gets or sets the no-code tracing configuration.
    /// </summary>
    [YamlMember(Alias = "no_code")]
    public NoCodeConfiguration? NoCode { get; set; }
}
