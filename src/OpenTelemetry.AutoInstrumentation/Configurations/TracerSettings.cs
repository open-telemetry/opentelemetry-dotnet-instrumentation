// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Tracer Settings
/// </summary>
internal class TracerSettings : Settings
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

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
    public IReadOnlyList<TracerInstrumentation> EnabledInstrumentations { get; private set; } = new List<TracerInstrumentation>();

    /// <summary>
    /// Gets the list of activity configurations to be added to the tracer at the startup.
    /// </summary>
    public IList<string> ActivitySources { get; } = new List<string> { "OpenTelemetry.AutoInstrumentation.*" };

    /// <summary>
    /// Gets the list of legacy configurations to be added to the tracer at the startup.
    /// </summary>
    public IList<string> AdditionalLegacySources { get; } = new List<string>();

    /// <summary>
    /// Gets the instrumentation options.
    /// </summary>
    public InstrumentationOptions InstrumentationOptions { get; private set; } = new(new Configuration(failFast: false));

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

        var instrumentationEnabledByDefault =
            configuration.GetBool(ConfigurationKeys.Traces.TracesInstrumentationEnabled) ??
            configuration.GetBool(ConfigurationKeys.InstrumentationEnabled) ?? true;

        EnabledInstrumentations = configuration.ParseEnabledEnumList<TracerInstrumentation>(
            enabledByDefault: instrumentationEnabledByDefault,
            enabledConfigurationTemplate: ConfigurationKeys.Traces.EnabledTracesInstrumentationTemplate);

        var additionalSources = configuration.GetString(ConfigurationKeys.Traces.AdditionalSources);
        if (additionalSources != null)
        {
            foreach (var configurationName in additionalSources.Split(Constants.ConfigurationValues.Separator))
            {
                ActivitySources.Add(configurationName);
            }
        }

        var additionalLegacySources = configuration.GetString(ConfigurationKeys.Traces.AdditionalLegacySources);
        if (additionalLegacySources != null)
        {
            foreach (var configurationName in additionalLegacySources.Split(Constants.ConfigurationValues.Separator))
            {
                AdditionalLegacySources.Add(configurationName);
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
                if (configuration.FailFast)
                {
                    var message = $"Traces exporter '{tracesExporterEnvVar}' is not supported.";
                    Logger.Error(message);
                    throw new NotSupportedException(message);
                }

                Logger.Error($"Traces exporter '{tracesExporterEnvVar}' is not supported. Defaulting to '{Constants.ConfigurationValues.Exporters.Otlp}'.");

                return TracesExporter.Otlp;
        }
    }
}
