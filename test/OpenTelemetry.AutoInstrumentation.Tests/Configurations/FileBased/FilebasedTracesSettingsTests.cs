// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.Trace;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased;

public class FilebasedTracesSettingsTests
{
    [Fact]
    public void LoadFile_SetsBatchProcessorAndExportersCorrectly()
    {
        var exporter = new ExporterConfig
        {
            OtlpGrpc = new OtlpGrpcExporterConfig
            {
                Endpoint = "http://localhost:4317/"
            },
            Zipkin = new ZipkinExporterConfig("http://localhost:9411/")
        };

        var batchProcessorConfig = new BatchProcessorConfig
        {
            ScheduleDelay = 1000,
            ExportTimeout = 30000,
            MaxQueueSize = 2048,
            MaxExportBatchSize = 512,
            Exporter = exporter
        };

        var conf = new Conf
        {
            TracerProvider = new TracerProviderConfiguration
            {
                Processors = new Dictionary<string, BatchProcessorConfig>
                {
                    { "batch", batchProcessorConfig }
                }
            }
        };

        var settings = new TracerSettings();

        settings.LoadFile(conf);

        Assert.True(settings.TracesEnabled);
        Assert.Equal(2, settings.TracesExporters.Count);
        Assert.Contains(TracesExporter.Otlp, settings.TracesExporters);
        Assert.Contains(TracesExporter.Zipkin, settings.TracesExporters);

        Assert.NotNull(settings.OtlpSettings);
        Assert.NotNull(settings.ZipkinSettings);

        Assert.NotNull(settings.OtlpSettings.Endpoint);
        Assert.Equal("http://localhost:4317/", settings.OtlpSettings.Endpoint.ToString());
        Assert.Equal("http://localhost:9411/", settings.ZipkinSettings.Endpoint);

        Assert.Equal(1000, settings.BatchProcessorConfig.ScheduleDelay);
        Assert.Equal(30000, settings.BatchProcessorConfig.ExportTimeout);
        Assert.Equal(2048, settings.BatchProcessorConfig.MaxQueueSize);
        Assert.Equal(512, settings.BatchProcessorConfig.MaxExportBatchSize);
    }

    [Fact]
    public void LoadFile_DisablesTraces_WhenNoBatchProcessorConfigured()
    {
        var conf = new Conf
        {
            TracerProvider = new TracerProviderConfiguration
            {
                Processors = []
            }
        };

        var settings = new TracerSettings();

        settings.LoadFile(conf);

        Assert.False(settings.TracesEnabled);
        Assert.Empty(settings.TracesExporters);
        Assert.Null(settings.OtlpSettings);
        Assert.Null(settings.ZipkinSettings);
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

        var batchProcessorConfig = new BatchProcessorConfig
        {
            Exporter = exporter
        };

        var conf = new Conf
        {
            TracerProvider = new TracerProviderConfiguration
            {
                Processors = new Dictionary<string, BatchProcessorConfig>
                {
                    { "batch", batchProcessorConfig }
                }
            }
        };

        var settings = new TracerSettings();

        settings.LoadFile(conf);

        Assert.True(settings.TracesEnabled);
        Assert.Single(settings.TracesExporters);
        Assert.Contains(TracesExporter.Otlp, settings.TracesExporters);
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

        var batchProcessorConfig = new BatchProcessorConfig
        {
            Exporter = exporter
        };

        var conf = new Conf
        {
            TracerProvider = new TracerProviderConfiguration
            {
                Processors = new Dictionary<string, BatchProcessorConfig>
                {
                    { "batch", batchProcessorConfig }
                }
            }
        };

        var settings = new TracerSettings();

        settings.LoadFile(conf);

        Assert.True(settings.TracesEnabled);
        Assert.Single(settings.TracesExporters);
        Assert.Contains(TracesExporter.Console, settings.TracesExporters);
        Assert.Null(settings.OtlpSettings);
        Assert.Null(settings.ZipkinSettings);
    }

    [Fact]
    public void LoadFile_SetsMultipleExportersCorrectly()
    {
        var exporter = new ExporterConfig
        {
            OtlpGrpc = new OtlpGrpcExporterConfig { Endpoint = "http://localhost:4317/" },
            Console = new object(),
            Zipkin = new ZipkinExporterConfig("http://localhost:9411/")
        };

        var batchProcessorConfig = new BatchProcessorConfig
        {
            Exporter = exporter
        };

        var conf = new Conf
        {
            TracerProvider = new TracerProviderConfiguration
            {
                Processors = new Dictionary<string, BatchProcessorConfig>
                {
                    { "batch", batchProcessorConfig }
                }
            }
        };

        var settings = new TracerSettings();

        settings.LoadFile(conf);

        Assert.True(settings.TracesEnabled);
        Assert.Equal(3, settings.TracesExporters.Count);
        Assert.Contains(TracesExporter.Otlp, settings.TracesExporters);
        Assert.Contains(TracesExporter.Console, settings.TracesExporters);
        Assert.Contains(TracesExporter.Zipkin, settings.TracesExporters);
    }

    [Fact]
    public void LoadFile_HandlesNullExporterGracefully()
    {
        var batchProcessorConfig = new BatchProcessorConfig
        {
            Exporter = null
        };

        var conf = new Conf
        {
            TracerProvider = new TracerProviderConfiguration
            {
                Processors = new Dictionary<string, BatchProcessorConfig>
                {
                    { "batch", batchProcessorConfig }
                }
            }
        };

        var settings = new TracerSettings();

        settings.LoadFile(conf);

        Assert.True(settings.TracesEnabled);
        Assert.Empty(settings.TracesExporters);
        Assert.Null(settings.OtlpSettings);
        Assert.Null(settings.ZipkinSettings);
    }

    [Fact]
    public void LoadFile_SetsEnabledInstrumentations_IfPresent()
    {
        var instrumentation = new DotNetInstrumentation
        {
            Traces = new DotNetTraces
            {
                Azure = new object(),
                Elasticsearch = new object()
            }
        };

        var conf = new Conf
        {
            InstrumentationDevelopment = new InstrumentationDevelopment
            {
                DotNet = instrumentation
            }
        };

        var settings = new TracerSettings();

        settings.LoadFile(conf);

        Assert.NotNull(settings.EnabledInstrumentations);
        Assert.Contains(TracerInstrumentation.Azure, settings.EnabledInstrumentations);
        Assert.Contains(TracerInstrumentation.Elasticsearch, settings.EnabledInstrumentations);
    }
}
