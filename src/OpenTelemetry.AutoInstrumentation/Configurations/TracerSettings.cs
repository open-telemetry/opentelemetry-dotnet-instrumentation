// <copyright file="TracerSettings.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Tracer Settings
/// </summary>
internal class TracerSettings : Settings
{
    /// <summary>
    /// Gets a value indicating whether the tracer should be loaded by the profiler. Default is true.
    /// </summary>
    public bool TracesEnabled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the OpenTracing tracer is enabled. Default is false.
    /// </summary>
    public bool OpenTracingEnabled { get; private set; }

    /// <summary>
    /// Gets the traces exporter.
    /// </summary>
    public TracesExporter TracesExporter { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the console exporter is enabled.
    /// </summary>
    public bool ConsoleExporterEnabled { get; private set; }

    /// <summary>
    /// Gets the list of enabled instrumentations.
    /// </summary>
    public IList<TracerInstrumentation> EnabledInstrumentations { get; private set; } = new List<TracerInstrumentation>();

    /// <summary>
    /// Gets the list of activity configurations to be added to the tracer at the startup.
    /// </summary>
    public IList<string> ActivitySources { get; } = new List<string> { "OpenTelemetry.AutoInstrumentation.*" };

    /// <summary>
    /// Gets the list of legacy configurations to be added to the tracer at the startup.
    /// </summary>
    public IList<string> LegacySources { get; } = new List<string>();

    /// <summary>
    /// Gets the instrumentation options.
    /// </summary>
    public InstrumentationOptions InstrumentationOptions { get; private set; } = new(new Configuration());

    /// <summary>
    /// Gets sampler to be used for traces.
    /// </summary>
    public string? TracesSampler { get; private set; }

    /// <summary>
    /// Gets a value to be used as the sampler argument.
    /// </summary>
    public string? TracesSamplerArguments { get; private set; }

    protected override void OnLoad(Configuration configuration)
    {
        TracesExporter = ParseTracesExporter(configuration);
        ConsoleExporterEnabled = configuration.GetBool(ConfigurationKeys.Traces.ConsoleExporterEnabled) ?? false;

        var instrumentationDisabledByDefault =
            configuration.GetBool(ConfigurationKeys.Traces.TracesInstrumentationDisabled) ??
            configuration.GetBool(ConfigurationKeys.InstrumentationDisabled) ?? false;

        EnabledInstrumentations = configuration.ParseEnabledEnumList<TracerInstrumentation>(
            disabledByDefault: instrumentationDisabledByDefault,
            disabledConfigurationTemplate: ConfigurationKeys.Traces.DisabledTracesInstrumentationTemplate);

        var additionalSources = configuration.GetString(ConfigurationKeys.Traces.AdditionalSources);
        if (additionalSources != null)
        {
            foreach (var configurationName in additionalSources.Split(Constants.ConfigurationValues.Separator))
            {
                ActivitySources.Add(configurationName);
            }
        }

        var legacySources = configuration.GetString(ConfigurationKeys.Traces.LegacySources);
        if (legacySources != null)
        {
            foreach (var configurationName in legacySources.Split(Constants.ConfigurationValues.Separator))
            {
                LegacySources.Add(configurationName);
            }
        }

        TracesEnabled = configuration.GetBool(ConfigurationKeys.Traces.TracesEnabled) ?? true;
        OpenTracingEnabled = configuration.GetBool(ConfigurationKeys.Traces.OpenTracingEnabled) ?? false;

        InstrumentationOptions = new InstrumentationOptions(configuration);

        TracesSampler = configuration.GetString(ConfigurationKeys.Traces.TracesSampler);
        TracesSamplerArguments = configuration.GetString(ConfigurationKeys.Traces.TracesSamplerArguments);
    }

    private static TracesExporter ParseTracesExporter(Configuration configuration)
    {
        var tracesExporterEnvVar = configuration.GetString(ConfigurationKeys.Traces.Exporter)
            ?? Constants.ConfigurationValues.Exporters.Otlp;

        switch (tracesExporterEnvVar)
        {
            case null:
            case "":
            case Constants.ConfigurationValues.Exporters.Otlp:
                return TracesExporter.Otlp;
            case Constants.ConfigurationValues.Exporters.Zipkin:
                return TracesExporter.Zipkin;
            case Constants.ConfigurationValues.None:
                return TracesExporter.None;
            default:
                throw new FormatException($"Traces exporter '{tracesExporterEnvVar}' is not supported");
        }
    }
}
