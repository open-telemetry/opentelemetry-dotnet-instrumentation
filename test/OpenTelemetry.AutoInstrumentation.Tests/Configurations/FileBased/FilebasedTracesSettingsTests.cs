// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased;

public class FilebasedTracesSettingsTests
{
    [Fact]
    public void LoadFile_SetsBatchProcessorAndExportersCorrectly()
    {
        var exporter1 = new BatchTracerExporterConfig
        {
            OtlpGrpc = new OtlpGrpcExporterConfig
            {
                Endpoint = "http://localhost:4317/"
            }
        };

        var exporter2 = new BatchTracerExporterConfig
        {
            Zipkin = new ZipkinExporterConfig
            {
                Endpoint = "http://localhost:9411/"
            }
        };

        var batchProcessorConfig1 = new BatchProcessorConfig
        {
            ScheduleDelay = 1000,
            ExportTimeout = 30000,
            MaxQueueSize = 2048,
            MaxExportBatchSize = 512,
            Exporter = exporter1
        };

        var batchProcessorConfig2 = new BatchProcessorConfig
        {
            Exporter = exporter2
        };

        var conf = new YamlConfiguration
        {
            TracerProvider = new TracerProviderConfiguration
            {
                Processors =
                [
                    new ProcessorConfig
                    {
                        Batch = batchProcessorConfig1
                    },
                    new ProcessorConfig
                    {
                        Batch = batchProcessorConfig2
                    }
                ]
            }
        };

        var settings = new TracerSettings();

        settings.LoadFile(conf);

        Assert.True(settings.TracesEnabled);
        Assert.NotNull(settings.Processors);
        Assert.Equal(2, settings.Processors.Count);

        Assert.Empty(settings.TracesExporters);
    }

    [Fact]
    public void LoadFile_DisablesTraces_WhenNoBatchProcessorConfigured()
    {
        var conf = new YamlConfiguration
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
        Assert.NotNull(settings.Processors);
        Assert.Empty(settings.Processors);
    }

    [Fact]
    public void LoadFile_HandlesNullExporterGracefully()
    {
        var batchProcessorConfig = new BatchProcessorConfig
        {
            Exporter = null
        };

        var conf = new YamlConfiguration
        {
            TracerProvider = new TracerProviderConfiguration
            {
                Processors =
                [
                    new ProcessorConfig
                    {
                        Batch = batchProcessorConfig
                    }
                ]
            }
        };

        var settings = new TracerSettings();

        settings.LoadFile(conf);

        Assert.True(settings.TracesEnabled);
        Assert.Empty(settings.TracesExporters);
    }
}
