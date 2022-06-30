// <copyright file="MeterSettings.cs" company="OpenTelemetry Authors">
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenTelemetry.AutoInstrumentation.Configuration
{
    /// <summary>
    /// Meter Settings
    /// </summary>
    public class MeterSettings : Settings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MeterSettings"/> class
        /// using the specified <see cref="IConfigurationSource"/> to initialize values.
        /// </summary>
        /// <param name="source">The <see cref="IConfigurationSource"/> to use when retrieving configuration values.</param>
        private MeterSettings(IConfigurationSource source)
            : base(source)
        {
            MetricExporter = ParseMetricExporter(source);
            ConsoleExporterEnabled = source.GetBool(ConfigurationKeys.Metrics.ConsoleExporterEnabled) ?? false;

            var instrumentations = new Dictionary<string, MeterInstrumentation>();
            var enabledInstrumentations = source.GetString(ConfigurationKeys.Metrics.Instrumentations);
            if (enabledInstrumentations != null)
            {
                foreach (var instrumentation in enabledInstrumentations.Split(Separator))
                {
                    if (Enum.TryParse(instrumentation, out MeterInstrumentation parsedType))
                    {
                        instrumentations[instrumentation] = parsedType;
                    }
                    else
                    {
                        throw new FormatException($"The \"{instrumentation}\" is not recognized as supported metrics instrumentation and cannot be enabled");
                    }
                }
            }

            var disabledInstrumentations = source.GetString(ConfigurationKeys.Metrics.DisabledInstrumentations);
            if (disabledInstrumentations != null)
            {
                foreach (var instrumentation in disabledInstrumentations.Split(Separator))
                {
                    instrumentations.Remove(instrumentation);
                }
            }

            EnabledInstrumentations = instrumentations.Values.ToList();

            var providerPlugins = source.GetString(ConfigurationKeys.Metrics.ProviderPlugins);
            if (providerPlugins != null)
            {
                foreach (var pluginAssemblyQualifiedName in providerPlugins.Split(DotNetQualifiedNameSeparator))
                {
                    MetricPlugins.Add(pluginAssemblyQualifiedName);
                }
            }

            var additionalSources = source.GetString(ConfigurationKeys.Metrics.AdditionalSources);
            if (additionalSources != null)
            {
                foreach (var sourceName in additionalSources.Split(Separator))
                {
                    Meters.Add(sourceName);
                }
            }

            MetricExportInterval = source.GetInt32(ConfigurationKeys.Metrics.ExportInterval);
            MetricsEnabled = source.GetBool(ConfigurationKeys.Metrics.Enabled) ?? true;
            LoadMetricsAtStartup = source.GetBool(ConfigurationKeys.Metrics.LoadMeterAtStartup) ?? true;
        }

        /// <summary>
        /// Gets a value indicating whether metrics is enabled.
        /// Default is <c>true</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.Metrics.Enabled"/>
        public bool MetricsEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether the metrics should be loaded by the profiler. Default is true.
        /// </summary>
        public bool LoadMetricsAtStartup { get; }

        /// <summary>
        /// Gets the metrics exporter.
        /// </summary>
        public MetricsExporter MetricExporter { get; }

        /// <summary>
        /// Gets the metrics export interval.
        /// </summary>
        public int? MetricExportInterval { get; }

        /// <summary>
        /// Gets a value indicating whether the console exporter is enabled.
        /// </summary>
        public bool ConsoleExporterEnabled { get; }

        /// <summary>
        /// Gets the list of plugins represented by <see cref="Type.AssemblyQualifiedName"/>.
        /// </summary>
        public IList<string> MetricPlugins { get; } = new List<string>();

        /// <summary>
        /// Gets the list of enabled meters.
        /// </summary>
        public IList<MeterInstrumentation> EnabledInstrumentations { get; }

        /// <summary>
        /// Gets the list of meters to be added to the MeterProvider at the startup.
        /// </summary>
        public IList<string> Meters { get; } = new List<string>();

        internal static MeterSettings FromDefaultSources()
        {
            var configurationSource = new CompositeConfigurationSource
            {
                new EnvironmentConfigurationSource(),

#if NETFRAMEWORK
                // on .NET Framework only, also read from app.config/web.config
                new NameValueConfigurationSource(System.Configuration.ConfigurationManager.AppSettings)
#endif
            };

            return new MeterSettings(configurationSource);
        }

        private static MetricsExporter ParseMetricExporter(IConfigurationSource source)
        {
            var metricsExporterEnvVar = source.GetString(ConfigurationKeys.Metrics.Exporter) ?? "otlp";
            switch (metricsExporterEnvVar)
            {
                case null:
                case "":
                case "otlp":
                    return MetricsExporter.Otlp;
                case "prometheus":
                    return MetricsExporter.Prometheus;
                case "none":
                    return MetricsExporter.None;
                default:
                    throw new FormatException($"Metric exporter '{metricsExporterEnvVar}' is not supported");
            }
        }
    }
}
