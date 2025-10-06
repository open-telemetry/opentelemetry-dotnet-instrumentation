// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class YamlConfiguration
{
    /// <summary>
    /// Gets or sets the file format version.
    /// The yaml format is documented at
    /// https://github.com/open-telemetry/opentelemetry-configuration/tree/main/schema
    /// </summary>
    [YamlMember(Alias = "file_format")]
    public string? FileFormat { get; set; }

    /// <summary>
    /// Gets or sets the resource configuration.
    /// Configure resource for all signals.
    /// If omitted, the default resource is used.
    /// </summary>
    [YamlMember(Alias = "resource")]
    public ResourceConfiguration? Resource { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the SDK is disabled.
    /// If omitted or null, false is used.
    /// </summary>
    [YamlMember(Alias = "disabled")]
    public bool Disabled { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the Fail Fast is enabled.
    /// If omitted or null, false is used.
    /// </summary>
    [YamlMember(Alias = "fail_fast")]
    public bool FailFast { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the Flush On Unhandled Exception is enabled.
    /// If omitted or null, false is used.
    /// </summary>
    [YamlMember(Alias = "flush_on_unhandled_exception")]
    public bool FlushOnUnhandledException { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether Profiling is enabled.
    /// If omitted or null, true is used.
    /// </summary>
    [YamlMember(Alias = "enable_profiling")]
    public bool EnableProfiling { get; set; } = true;
    /// Gets or sets the no-code tracing configuration.
    /// </summary>
    [YamlMember(Alias = "no_code/development")]
    public NoCodeConfiguration? NoCode { get; set; }
}
