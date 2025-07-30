// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased;

public class FilebasedLogsSettingsTests
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

        var batchProcessorConfig = new BatchProcessorConfig
        {
            ScheduleDelay = 2000,
            ExportTimeout = 10000,
            MaxQueueSize = 1024,
            MaxExportBatchSize = 256,
            Exporter = exporter
        };

        var conf = new Conf
        {
            LoggerProvider = new LoggerProviderConfiguration
            {
                Processors = new Dictionary<string, BatchProcessorConfig>
                {
                    { "batch", batchProcessorConfig }
                }
            }
        };

        var settings = new LogSettings();

        settings.LoadFile(conf);

        Assert.True(settings.LogsEnabled);
        Assert.Single(settings.LogExporters);
        Assert.Contains(LogExporter.Otlp, settings.LogExporters);

        Assert.NotNull(settings.OtlpSettings);
        Assert.NotNull(settings.OtlpSettings.Endpoint);
        Assert.Equal("http://localhost:4317/", settings.OtlpSettings.Endpoint.ToString());

        Assert.Equal(2000, settings.BatchProcessorConfig.ScheduleDelay);
        Assert.Equal(10000, settings.BatchProcessorConfig.ExportTimeout);
        Assert.Equal(1024, settings.BatchProcessorConfig.MaxQueueSize);
        Assert.Equal(256, settings.BatchProcessorConfig.MaxExportBatchSize);
    }

    [Fact]
    public void LoadFile_DisablesLogs_WhenNoBatchProcessorConfigured()
    {
        var conf = new Conf
        {
            LoggerProvider = new LoggerProviderConfiguration
            {
                Processors = []
            }
        };

        var settings = new LogSettings();

        settings.LoadFile(conf);

        Assert.False(settings.LogsEnabled);
        Assert.Empty(settings.LogExporters);
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

        var batchProcessorConfig = new BatchProcessorConfig
        {
            Exporter = exporter
        };

        var conf = new Conf
        {
            LoggerProvider = new LoggerProviderConfiguration
            {
                Processors = new Dictionary<string, BatchProcessorConfig>
            {
                { "batch", batchProcessorConfig }
            }
            }
        };

        var settings = new LogSettings();

        settings.LoadFile(conf);

        Assert.True(settings.LogsEnabled);
        Assert.Single(settings.LogExporters);
        Assert.Contains(LogExporter.Otlp, settings.LogExporters);
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
            LoggerProvider = new LoggerProviderConfiguration
            {
                Processors = new Dictionary<string, BatchProcessorConfig>
            {
                { "batch", batchProcessorConfig }
            }
            }
        };

        var settings = new LogSettings();

        settings.LoadFile(conf);

        Assert.True(settings.LogsEnabled);
        Assert.Single(settings.LogExporters);
        Assert.Contains(LogExporter.Console, settings.LogExporters);
        Assert.Null(settings.OtlpSettings);
    }

    [Fact]
    public void LoadFile_SetsMultipleExportersCorrectly()
    {
        var exporter = new ExporterConfig
        {
            OtlpGrpc = new OtlpGrpcExporterConfig { Endpoint = "http://localhost:4317/" },
            Console = new object()
        };

        var batchProcessorConfig = new BatchProcessorConfig
        {
            Exporter = exporter
        };

        var conf = new Conf
        {
            LoggerProvider = new LoggerProviderConfiguration
            {
                Processors = new Dictionary<string, BatchProcessorConfig>
            {
                { "batch", batchProcessorConfig }
            }
            }
        };

        var settings = new LogSettings();

        settings.LoadFile(conf);

        Assert.True(settings.LogsEnabled);
        Assert.Equal(2, settings.LogExporters.Count);
        Assert.Contains(LogExporter.Otlp, settings.LogExporters);
        Assert.Contains(LogExporter.Console, settings.LogExporters);
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
            LoggerProvider = new LoggerProviderConfiguration
            {
                Processors = new Dictionary<string, BatchProcessorConfig>
            {
                { "batch", batchProcessorConfig }
            }
            }
        };

        var settings = new LogSettings();

        settings.LoadFile(conf);

        Assert.True(settings.LogsEnabled);
        Assert.Empty(settings.LogExporters);
        Assert.Null(settings.OtlpSettings);
    }

    [Fact]
    public void LoadFile_SetsEnabledInstrumentations_IfPresent()
    {
        var instrumentation = new DotNetInstrumentation
        {
            Logs = new DotNetLogs
            {
                ILogger = new object(),
                Log4Net = new object(),
            }
        };

        var conf = new Conf
        {
            InstrumentationDevelopment = new InstrumentationDevelopment
            {
                DotNet = instrumentation
            }
        };

        var settings = new LogSettings();

        settings.LoadFile(conf);

        Assert.NotNull(settings.EnabledInstrumentations);
        Assert.Contains(LogInstrumentation.ILogger, settings.EnabledInstrumentations);
        Assert.Contains(LogInstrumentation.Log4Net, settings.EnabledInstrumentations);
    }
}
