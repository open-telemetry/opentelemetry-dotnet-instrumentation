// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased;

public class FilebasedMetricsSettingsTests
{
    [Fact]
    public void LoadFile_SetsPeriodicReadersCorrectly()
    {
        var readerConfig = new MetricReaderConfig
        {
            Periodic = new PeriodicMetricReaderConfig
            {
                Interval = 1000,
                Timeout = 2000,
                Exporter = new MetricPeriodicExporterConfig
                {
                    OtlpHttp = new OtlpHttpExporterConfig
                    {
                        Endpoint = "http://localhost:4318/v1/metrics"
                    }
                }
            }
        };

        var configuration = new YamlConfiguration
        {
            MeterProvider = new MeterProviderConfiguration
            {
                Readers = [readerConfig]
            }
        };

        var settings = new MetricSettings();

        settings.LoadFile(configuration);

        Assert.True(settings.MetricsEnabled);
        Assert.Empty(settings.MetricExporters);
        Assert.NotNull(settings.Readers);
        var readers = Assert.Single(settings.Readers);
        Assert.NotNull(readers.Periodic);
        Assert.NotNull(readers.Periodic!.Exporter);
        Assert.NotNull(readers.Periodic!.Exporter!.OtlpHttp);
    }

    [Fact]
    public void LoadFile_SetsPullReadersCorrectly()
    {
        var readerConfig = new MetricReaderConfig
        {
            Pull = new PullMetricReaderConfig
            {
                Exporter = new MetricPullExporterConfig
                {
                    Prometheus = new object()
                }
            }
        };

        var configuration = new YamlConfiguration
        {
            MeterProvider = new MeterProviderConfiguration
            {
                Readers = [readerConfig]
            }
        };

        var settings = new MetricSettings();

        settings.LoadFile(configuration);

        Assert.True(settings.MetricsEnabled);
        Assert.Empty(settings.MetricExporters);
        Assert.NotNull(settings.Readers);
        var readers = Assert.Single(settings.Readers);
        Assert.NotNull(readers.Pull);
        Assert.NotNull(readers.Pull!.Exporter);
        Assert.NotNull(readers.Pull!.Exporter!.Prometheus);
    }

    [Fact]
    public void LoadFile_DisablesMetrics_WhenNoReadersConfigured()
    {
        var configuration = new YamlConfiguration
        {
            MeterProvider = new MeterProviderConfiguration
            {
                Readers = []
            }
        };

        var settings = new MetricSettings();

        settings.LoadFile(configuration);

        Assert.False(settings.MetricsEnabled);
        Assert.Empty(settings.MetricExporters);
        Assert.NotNull(settings.Readers);
        Assert.Empty(settings.Readers!);
    }

    [Fact]
    public void LoadFile_HandlesNullExporterGracefully()
    {
        var readerConfig = new MetricReaderConfig
        {
            Periodic = new PeriodicMetricReaderConfig
            {
                Exporter = null
            }
        };

        var configuration = new YamlConfiguration
        {
            MeterProvider = new MeterProviderConfiguration
            {
                Readers = [readerConfig]
            }
        };

        var settings = new MetricSettings();

        settings.LoadFile(configuration);

        Assert.True(settings.MetricsEnabled);
        Assert.Empty(settings.MetricExporters);
        Assert.NotNull(settings.Readers);
    }
}
