// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased;

public class FilebasedLogsSettingsTests
{
    [Fact]
    public void LoadFile_SetsProcessorsCorrectly()
    {
        var batchProcessorConfig1 = new LogBatchProcessorConfig
        {
            ScheduleDelay = 1000,
            ExportTimeout = 30000,
            MaxQueueSize = 2048,
            MaxExportBatchSize = 512,
            Exporter = new BatchLogExporterConfig
            {
                OtlpGrpc = new OtlpGrpcExporterConfig
                {
                    Endpoint = "http://localhost:4317/"
                }
            }
        };

        var batchProcessorConfig2 = new LogBatchProcessorConfig
        {
            Exporter = new BatchLogExporterConfig
            {
                OtlpHttp = new OtlpHttpExporterConfig
                {
                    Endpoint = "http://localhost:4318/v1/logs"
                }
            }
        };

        var simpleProcessorConfig = new LogProcessorConfig
        {
            Simple = new LogSimpleProcessorConfig
            {
                Exporter = new SimpleLogExporterConfig
                {
                    Console = new object()
                }
            }
        };

        var conf = new YamlConfiguration
        {
            LoggerProvider = new LoggerProviderConfiguration
            {
                Processors =
                [
                    new LogProcessorConfig
                    {
                        Batch = batchProcessorConfig1
                    },
                    new LogProcessorConfig
                    {
                        Batch = batchProcessorConfig2
                    },
                    simpleProcessorConfig
                ]
            }
        };

        var settings = new LogSettings();

        settings.LoadFile(conf);

        Assert.True(settings.LogsEnabled);
        Assert.NotNull(settings.Processors);
        Assert.Equal(3, settings.Processors.Count);
        Assert.Empty(settings.LogExporters);
    }

    [Fact]
    public void LoadFile_DisablesLogs_WhenNoProcessorConfigured()
    {
        var conf = new YamlConfiguration
        {
            LoggerProvider = new LoggerProviderConfiguration
            {
                Processors = []
            }
        };

        var settings = new LogSettings();

        settings.LoadFile(conf);

        Assert.False(settings.LogsEnabled);
        Assert.NotNull(settings.Processors);
        Assert.Empty(settings.Processors);
        Assert.Empty(settings.LogExporters);
    }

    [Fact]
    public void LoadFile_HandlesNullExporterGracefully()
    {
        var batchProcessorConfig = new LogBatchProcessorConfig
        {
            Exporter = null
        };

        var conf = new YamlConfiguration
        {
            LoggerProvider = new LoggerProviderConfiguration
            {
                Processors =
                [
                    new LogProcessorConfig
                    {
                        Batch = batchProcessorConfig
                    }
                ]
            }
        };

        var settings = new LogSettings();

        settings.LoadFile(conf);

        Assert.True(settings.LogsEnabled);
        Assert.NotNull(settings.Processors);
        Assert.Single(settings.Processors);
        Assert.Empty(settings.LogExporters);
    }
}
