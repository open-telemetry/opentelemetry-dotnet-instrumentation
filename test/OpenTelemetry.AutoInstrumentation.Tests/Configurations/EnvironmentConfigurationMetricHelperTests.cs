// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.AutoInstrumentation.Loading;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.Metrics;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

public class EnvironmentConfigurationMetricHelperTests
{
    [Fact]
    public void UseEnvironmentVariables_ConfiguresConsoleMetricReaderFromFile()
    {
        const int exportIntervalMilliseconds = 12345;
        const int exportTimeoutMilliseconds = 6789;

        var periodicReader = new PeriodicMetricReaderConfig
        {
            Interval = exportIntervalMilliseconds,
            Timeout = exportTimeoutMilliseconds,
            Exporter = new MetricPeriodicExporterConfig
            {
                Console = new ConsoleExporterConfig
                {
                    TemporalityPreference = "delta"
                }
            }
        };
        var configuration = new YamlConfiguration
        {
            MeterProvider = new MeterProviderConfiguration
            {
                Readers = [new MetricReaderConfig { Periodic = periodicReader }]
            },
            Plugins = new PluginsConfiguration
            {
                Plugins = [typeof(MetricReaderOptionsCapturingPlugin).AssemblyQualifiedName!]
            }
        };

        var metricSettings = new MetricSettings();
        metricSettings.LoadFile(configuration);
        var pluginSettings = new PluginsSettings();
        pluginSettings.LoadFile(configuration);
        var pluginManager = new PluginManager(pluginSettings);
        using var lazyInstrumentationLoader = new LazyInstrumentationLoader();

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .UseEnvironmentVariables(lazyInstrumentationLoader, metricSettings, pluginManager)
            .Build();

        var plugin = Assert.IsType<MetricReaderOptionsCapturingPlugin>(Assert.Single(pluginManager.Plugins).Instance);
        var options = Assert.IsType<MetricReaderOptions>(plugin.Options);

        Assert.Equal(exportIntervalMilliseconds, options.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds);
        Assert.Equal(exportTimeoutMilliseconds, options.PeriodicExportingMetricReaderOptions.ExportTimeoutMilliseconds);
        Assert.Equal(MetricReaderTemporalityPreference.Delta, options.TemporalityPreference);
    }

#pragma warning disable CA1515 // Consider making public types internal. Needed for plugin purposes.
#pragma warning disable CA1034 // Nested types should not be visible. It is used only for test purposes.
    public class MetricReaderOptionsCapturingPlugin
#pragma warning restore CA1034 // Nested types should not be visible. It is used only for test purposes.
#pragma warning restore CA1515 // Consider making public types internal. Needed for plugin purposes.
    {
        public MetricReaderOptions? Options { get; private set; }

        public void ConfigureMetricsOptions(MetricReaderOptions options)
        {
            Options = options;
        }
    }
}
