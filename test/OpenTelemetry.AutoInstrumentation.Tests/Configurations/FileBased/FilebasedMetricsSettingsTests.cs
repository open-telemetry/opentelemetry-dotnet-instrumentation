// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.Trace;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased;

public class FilebasedMetricsSettingsTests
{
    [Fact]
    public void LoadFile_SetsBatchProcessorAndExportersCorrectly()
    {
        var exporter = new ExporterConfig
        {
            OtlpGrpc = new OtlpGrpcExporterConfig
            {
                Endpoint = "http://localhost:4317/"
            }
        };

        var readerConfig = new ReaderConfiguration
        {
            Interval = 1000,
            Timeout = 30000,
            Exporter = exporter
        };

        var conf = new Conf
        {
            MeterProvider = new MeterProviderConfiguration
            {
                Readers = new Dictionary<string, ReaderConfiguration>
                {
                    { "periodic", readerConfig }
                }
            }
        };

        var settings = new MetricSettings();
        settings.LoadFile(conf);

        Assert.True(settings.MetricsEnabled);
        Assert.Single(settings.MetricExporters);
        Assert.Contains(MetricsExporter.Otlp, settings.MetricExporters);
        Assert.NotNull(settings.OtlpSettings);
        Assert.NotNull(settings.OtlpSettings.Endpoint);
        Assert.Equal("http://localhost:4317/", settings.OtlpSettings.Endpoint.ToString());

        Assert.Equal(1000, settings.ReaderConfiguration.Interval);
        Assert.Equal(30000, settings.ReaderConfiguration.Timeout);
    }

    [Fact]
    public void LoadFile_DisablesMetrics_WhenNoReadersConfigured()
    {
        var conf = new Conf
        {
            MeterProvider = new MeterProviderConfiguration
            {
                Readers = []
            }
        };

        var settings = new MetricSettings();
        settings.LoadFile(conf);

        Assert.False(settings.MetricsEnabled);
        Assert.Empty(settings.MetricExporters);
        Assert.Null(settings.OtlpSettings);
    }

    [Fact]
    public void LoadFile_SetsOtlpHttpExporterCorrectly()
    {
        var exporter = new ExporterConfig
        {
            OtlpHttp = new OtlpHttpExporterConfig
            {
                Endpoint = "http://localhost:4318/"
            }
        };

        var readerConfig = new ReaderConfiguration
        {
            Exporter = exporter
        };

        var conf = new Conf
        {
            MeterProvider = new MeterProviderConfiguration
            {
                Readers = new Dictionary<string, ReaderConfiguration>
                {
                    { "periodic", readerConfig }
                }
            }
        };

        var settings = new MetricSettings();
        settings.LoadFile(conf);

        Assert.True(settings.MetricsEnabled);
        Assert.Single(settings.MetricExporters);
        Assert.Contains(MetricsExporter.Otlp, settings.MetricExporters);
        Assert.NotNull(settings.OtlpSettings);
        Assert.NotNull(settings.OtlpSettings.Endpoint);
        Assert.Equal("http://localhost:4318/", settings.OtlpSettings.Endpoint.ToString());
    }

    [Fact]
    public void LoadFile_SetsConsoleExporterCorrectly()
    {
        var exporter = new ExporterConfig
        {
            Console = new object()
        };

        var readerConfig = new ReaderConfiguration
        {
            Exporter = exporter
        };

        var conf = new Conf
        {
            MeterProvider = new MeterProviderConfiguration
            {
                Readers = new Dictionary<string, ReaderConfiguration>
                {
                    { "periodic", readerConfig }
                }
            }
        };

        var settings = new MetricSettings();
        settings.LoadFile(conf);

        Assert.True(settings.MetricsEnabled);
        Assert.Single(settings.MetricExporters);
        Assert.Contains(MetricsExporter.Console, settings.MetricExporters);
        Assert.Null(settings.OtlpSettings);
    }

    [Fact]
    public void LoadFile_SetsPrometheusExporterCorrectly_FromPullReader()
    {
        var exporter = new ExporterConfig
        {
            Prometheus = new object()
        };

        var pullReader = new ReaderConfiguration
        {
            Exporter = exporter
        };

        var conf = new Conf
        {
            MeterProvider = new MeterProviderConfiguration
            {
                Readers = new Dictionary<string, ReaderConfiguration>
                {
                    { "pull", pullReader }
                }
            }
        };

        var settings = new MetricSettings();
        settings.LoadFile(conf);

        Assert.True(settings.MetricsEnabled);
        Assert.Single(settings.MetricExporters);
        Assert.Contains(MetricsExporter.Prometheus, settings.MetricExporters);
    }

    [Fact]
    public void LoadFile_SetsMultipleExportersCorrectly()
    {
        var exporter = new ExporterConfig
        {
            OtlpGrpc = new OtlpGrpcExporterConfig { Endpoint = "http://localhost:4317/" },
            Console = new object()
        };

        var periodicReader = new ReaderConfiguration
        {
            Exporter = exporter
        };

        var pullReader = new ReaderConfiguration
        {
            Exporter = new ExporterConfig
            {
                Prometheus = new object()
            }
        };

        var conf = new Conf
        {
            MeterProvider = new MeterProviderConfiguration
            {
                Readers = new Dictionary<string, ReaderConfiguration>
                {
                    { "periodic", periodicReader },
                    { "pull", pullReader }
                }
            }
        };

        var settings = new MetricSettings();
        settings.LoadFile(conf);

        Assert.True(settings.MetricsEnabled);
        Assert.Equal(3, settings.MetricExporters.Count);
        Assert.Contains(MetricsExporter.Otlp, settings.MetricExporters);
        Assert.Contains(MetricsExporter.Console, settings.MetricExporters);
        Assert.Contains(MetricsExporter.Prometheus, settings.MetricExporters);
    }

    [Fact]
    public void LoadFile_HandlesNullExporterGracefully()
    {
        var readerConfig = new ReaderConfiguration
        {
            Exporter = null
        };

        var conf = new Conf
        {
            MeterProvider = new MeterProviderConfiguration
            {
                Readers = new Dictionary<string, ReaderConfiguration>
                {
                    { "periodic", readerConfig }
                }
            }
        };

        var settings = new MetricSettings();
        settings.LoadFile(conf);

        Assert.True(settings.MetricsEnabled);
        Assert.Empty(settings.MetricExporters);
        Assert.Null(settings.OtlpSettings);
    }

    [Fact]
    public void LoadFile_SetsEnabledInstrumentations_IfPresent()
    {
        var instrumentation = new DotNetInstrumentation
        {
            Metrics = new DotNetMetrics
            {
                HttpClient = new object(),
                NetRuntime = new object()
            }
        };

        var conf = new Conf
        {
            InstrumentationDevelopment = new InstrumentationDevelopment
            {
                DotNet = instrumentation
            }
        };

        var settings = new MetricSettings();
        settings.LoadFile(conf);

        Assert.NotNull(settings.EnabledInstrumentations);
        Assert.Contains(MetricInstrumentation.HttpClient, settings.EnabledInstrumentations);
        Assert.Contains(MetricInstrumentation.NetRuntime, settings.EnabledInstrumentations);
    }
}
