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
    /// Gets the list of enabled traces exporters.
    /// </summary>
    public IReadOnlyList<TracesExporter> TracesExporters { get; private set; } = new List<TracesExporter>();

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

    protected override void OnLoad(Configuration configuration)
    {
        TracesExporters = ParseTracesExporter(configuration);
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
    }

    private static IReadOnlyList<TracesExporter> ParseTracesExporter(Configuration configuration)
    {
        var tracesExporterEnvVar = configuration.GetString(ConfigurationKeys.Traces.Exporter);

        if (string.IsNullOrWhiteSpace(tracesExporterEnvVar))
        {
            tracesExporterEnvVar = Constants.ConfigurationValues.Exporters.Otlp;
        }

        var exporters = new HashSet<TracesExporter>();

        var exporterNames = tracesExporterEnvVar?.ToLower()
                                                 .Split(',')
                                                 .Select(e => e.Trim())
                                                 .Where(e => !string.IsNullOrEmpty(e))
                                                 .ToList();

        if (exporterNames != null)
        {
            var hasExporter = exporterNames.Count > 1;

            foreach (var exporterName in exporterNames)
            {
                switch (exporterName)
                {
                    case Constants.ConfigurationValues.Exporters.Otlp:
                        exporters.Add(TracesExporter.Otlp);
                        break;
                    case Constants.ConfigurationValues.Exporters.Zipkin:
                        exporters.Add(TracesExporter.Zipkin);
                        break;
                    case Constants.ConfigurationValues.None:
                        if (!hasExporter)
                        {
                            exporters.Add(TracesExporter.None);
                        }

                        break;
                    default:
                        if (configuration.FailFast)
                        {
                            var message = $"Traces exporter '{exporterName}' is not supported.";
                            Logger.Error(message);
                            throw new NotSupportedException(message);
                        }

                        Logger.Error($"Traces exporter '{exporterName}' is not supported. Defaulting to '{Constants.ConfigurationValues.Exporters.Otlp}'.");
                        exporters.Add(TracesExporter.Otlp);
                        hasExporter = true;
                        break;
                }
            }
        }
        else
        {
            exporters.Add(TracesExporter.Otlp);
        }

        return exporters.ToList().AsReadOnly();
    }
}
