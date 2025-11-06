// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
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
    /// Gets or sets the instrumentation development configuration.
    /// Configure instrumentation.
    /// This type is in development and subject to breaking changes in minor versions.
    /// </summary>
    [YamlMember(Alias = "instrumentation/development")]
    public InstrumentationDevelopment? InstrumentationDevelopment { get; set; }

    /// <summary>
    /// Gets or sets the tracer provider configuration.
    /// Configure tracer provider.
    /// If omitted, a noop tracer provider is used.
    /// </summary>
    [YamlMember(Alias = "tracer_provider")]
    public TracerProviderConfiguration? TracerProvider { get; set; }

    /// <summary>
    /// Gets or sets the meter provider configuration.
    /// Configure meter provider.
    /// If omitted, a noop meter provider is used.
    /// </summary>
    [YamlMember(Alias = "meter_provider")]
    public MeterProviderConfiguration? MeterProvider { get; set; }

    /// <summary>
    /// Gets or sets the logger provider configuration.
    /// Configure logger provider.
    /// If omitted, a noop logger provider is used.
    /// </summary>
    [YamlMember(Alias = "logger_provider")]
    public LoggerProviderConfiguration? LoggerProvider { get; set; }

    /// <summary>
    /// Gets or sets the text map context propagator configuration.
    /// If omitted, a noop propagator is used.
    /// </summary>
    [YamlMember(Alias = "propagator")]
    public PropagatorConfiguration? Propagator { get; set; }

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
    /// Gets or sets a value indicating whether the Logs Include Formatted Message is enabled.
    /// If omitted or null, false is used.
    /// </summary>
    [YamlMember(Alias = "logs_include_formatted_message")]
    public bool LogsIncludeFormattedMessage { get; set; } = false;

    /// <summary>
    /// Gets or sets the plugins configuration.
    /// </summary>
    [YamlMember(Alias = "plugins/development")]
    public PluginsConfiguration? Plugins { get; set; }
}
