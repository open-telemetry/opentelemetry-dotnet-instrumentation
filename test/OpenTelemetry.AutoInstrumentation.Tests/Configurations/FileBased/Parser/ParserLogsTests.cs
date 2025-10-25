// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Xunit;
using YamlParser = OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser.Parser;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased.Parser;

[Collection("Non-Parallel Collection")]
public class ParserLogsTests
{
    [Fact]
    public void Parse_FullConfigYaml_ShouldPopulateModelCorrectly()
    {
        var config = YamlParser.ParseYaml("Configurations/FileBased/Files/TestLogsFile.yaml");
        Assert.NotNull(config);

        Assert.NotNull(config.LoggerProvider);
        Assert.NotNull(config.LoggerProvider.Processors);
        Assert.Equal(3, config.LoggerProvider.Processors.Count);

        var batchProcessor = config.LoggerProvider.Processors[0].Batch;
        Assert.NotNull(batchProcessor);
        Assert.Equal(5000, batchProcessor.ScheduleDelay);
        Assert.Equal(30000, batchProcessor.ExportTimeout);
        Assert.Equal(2048, batchProcessor.MaxQueueSize);
        Assert.Equal(512, batchProcessor.MaxExportBatchSize);

        Assert.NotNull(batchProcessor.Exporter);
        var httpExporter = batchProcessor.Exporter.OtlpHttp;
        Assert.NotNull(httpExporter);
        Assert.Equal("http://localhost:4318/v1/logs", httpExporter.Endpoint);
        Assert.Equal(10000, httpExporter.Timeout);

        Assert.NotNull(httpExporter.Headers);
        Assert.Single(httpExporter.Headers);
        Assert.Equal("api-key", httpExporter.Headers[0].Name);
        Assert.Equal("1234", httpExporter.Headers[0].Value);
        Assert.Equal("api-key=1234", httpExporter.HeadersList);

        var grpcProcessor = config.LoggerProvider.Processors[1].Batch;
        Assert.NotNull(grpcProcessor);
        Assert.NotNull(grpcProcessor.Exporter);
        var grpcExporter = grpcProcessor.Exporter.OtlpGrpc;
        Assert.NotNull(grpcExporter);
        Assert.Equal("http://localhost:4317", grpcExporter.Endpoint);
        Assert.NotNull(grpcExporter.Headers);
        Assert.Single(grpcExporter.Headers);
        Assert.Equal("api-key", grpcExporter.Headers[0].Name);
        Assert.Equal("1234", grpcExporter.Headers[0].Value);
        Assert.Equal("api-key=1234", grpcExporter.HeadersList);

        var simpleProcessor = config.LoggerProvider.Processors[2].Simple;
        Assert.NotNull(simpleProcessor);
        Assert.NotNull(simpleProcessor.Exporter);
        Assert.NotNull(simpleProcessor.Exporter.Console);
    }

    [Fact]
    public void Parse_EnvVarYaml_ShouldPopulateModelCompletely()
    {
        Environment.SetEnvironmentVariable("OTEL_BSP_SCHEDULE_DELAY", "7000");
        Environment.SetEnvironmentVariable("OTEL_BSP_EXPORT_TIMEOUT", "35000");
        Environment.SetEnvironmentVariable("OTEL_BSP_MAX_QUEUE_SIZE", "4096");
        Environment.SetEnvironmentVariable("OTEL_BSP_MAX_EXPORT_BATCH_SIZE", "1024");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", "http://collector:4318/v1/logs");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_TIMEOUT", "15000");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_HEADERS", "header1=value1,header2=value2");

        var config = YamlParser.ParseYaml("Configurations/FileBased/Files/TestLogsFileEnvVars.yaml");
        Assert.NotNull(config);

        Assert.NotNull(config.LoggerProvider);
        Assert.NotNull(config.LoggerProvider.Processors);
        var batchProcessor = config.LoggerProvider.Processors[0].Batch;
        Assert.NotNull(batchProcessor);
        Assert.Equal(7000, batchProcessor.ScheduleDelay);
        Assert.Equal(35000, batchProcessor.ExportTimeout);
        Assert.Equal(4096, batchProcessor.MaxQueueSize);
        Assert.Equal(1024, batchProcessor.MaxExportBatchSize);

        Assert.NotNull(batchProcessor.Exporter);
        var httpExporter = batchProcessor.Exporter.OtlpHttp;
        Assert.NotNull(httpExporter);
        Assert.Equal("http://collector:4318/v1/logs", httpExporter.Endpoint);
        Assert.Equal(15000, httpExporter.Timeout);
        Assert.Null(httpExporter.Headers);
        Assert.Equal("header1=value1,header2=value2", httpExporter.HeadersList);
    }
}
