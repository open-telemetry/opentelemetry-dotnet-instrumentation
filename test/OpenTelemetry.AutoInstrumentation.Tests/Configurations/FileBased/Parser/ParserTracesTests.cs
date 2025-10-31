// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Xunit;
using YamlParser = OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser.Parser;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased.Parser;

[Collection("Non-Parallel Collection")]
public class ParserTracesTests
{
    [Fact]
    public void Parse_FullConfigYaml_ShouldPopulateModelCorrectly()
    {
        var config = YamlParser.ParseYaml("Configurations/FileBased/Files/TestTracesFile.yaml");
        Assert.NotNull(config);

        Assert.NotNull(config.TracerProvider);
        Assert.NotNull(config.TracerProvider.Processors);
        Assert.Equal(4, config.TracerProvider.Processors.Count);

        var traceBatch = config.TracerProvider.Processors[0].Batch;
        Assert.NotNull(traceBatch);

        Assert.Equal(5000, traceBatch.ScheduleDelay);
        Assert.Equal(30000, traceBatch.ExportTimeout);
        Assert.Equal(2048, traceBatch.MaxQueueSize);
        Assert.Equal(512, traceBatch.MaxExportBatchSize);

        Assert.NotNull(traceBatch.Exporter);
        var traceExporter = traceBatch.Exporter.OtlpHttp;
        Assert.NotNull(traceExporter);

        Assert.Equal("http://localhost:4318/v1/traces", traceExporter.Endpoint);
        Assert.Equal(10000, traceExporter.Timeout);

        Assert.NotNull(traceExporter.Headers);
        Assert.Single(traceExporter.Headers);
        Assert.Equal("header1234", traceExporter.Headers[0].Name);
        Assert.Equal("1234", traceExporter.Headers[0].Value);

        var zipkinBatch = config.TracerProvider.Processors[1].Batch;
        Assert.NotNull(zipkinBatch);
        Assert.NotNull(zipkinBatch.Exporter);
        var zipkinExporter = zipkinBatch.Exporter.Zipkin;
        Assert.NotNull(zipkinExporter);
        Assert.Equal("http://localhost:9411/zipkin", zipkinExporter.Endpoint);

        var grpcBatch = config.TracerProvider.Processors[2].Batch;
        Assert.NotNull(grpcBatch);
        Assert.NotNull(grpcBatch.Exporter);
        var grpcExporter = grpcBatch.Exporter.OtlpGrpc;
        Assert.NotNull(grpcExporter);
        Assert.Equal("http://localhost:4317", grpcExporter.Endpoint);

        var simpleProcessor = config.TracerProvider.Processors[3].Simple;
        Assert.NotNull(simpleProcessor);
        Assert.NotNull(simpleProcessor.Exporter);
        Assert.NotNull(simpleProcessor.Exporter.Console);

        var sampler = config.TracerProvider.Sampler;
        Assert.NotNull(sampler);
        Assert.NotNull(sampler.ParentBased);
        Assert.NotNull(sampler.ParentBased.Root);
        Assert.NotNull(sampler.ParentBased.Root.AlwaysOn);
        Assert.NotNull(sampler.ParentBased.RemoteParentSampled);
        Assert.NotNull(sampler.ParentBased.RemoteParentSampled.AlwaysOn);
        Assert.NotNull(sampler.ParentBased.RemoteParentNotSampled);
        Assert.NotNull(sampler.ParentBased.RemoteParentNotSampled.AlwaysOff);
        Assert.NotNull(sampler.ParentBased.LocalParentSampled);
        Assert.NotNull(sampler.ParentBased.LocalParentSampled.AlwaysOn);
        Assert.NotNull(sampler.ParentBased.LocalParentNotSampled);
        Assert.NotNull(sampler.ParentBased.LocalParentNotSampled.AlwaysOff);
    }

    [Fact]
    public void Parse_EnvVarYaml_ShouldPopulateModelCompletely()
    {
        Environment.SetEnvironmentVariable("OTEL_SDK_DISABLED", "true");
        Environment.SetEnvironmentVariable("OTEL_BSP_SCHEDULE_DELAY", "7000");
        Environment.SetEnvironmentVariable("OTEL_BSP_EXPORT_TIMEOUT", "35000");
        Environment.SetEnvironmentVariable("OTEL_BSP_MAX_QUEUE_SIZE", "4096");
        Environment.SetEnvironmentVariable("OTEL_BSP_MAX_EXPORT_BATCH_SIZE", "1024");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "http://collector:4318/v1/traces");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_TIMEOUT", "15000");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_HEADERS", "header1=value1,header2=value2");

        var config = YamlParser.ParseYaml("Configurations/FileBased/Files/TestTracesFileEnvVars.yaml");
        Assert.NotNull(config);

        Assert.NotNull(config.TracerProvider);
        Assert.NotNull(config.TracerProvider.Processors);
        var traceBatch = config.TracerProvider.Processors[0].Batch;
        Assert.NotNull(traceBatch);

        Assert.NotNull(traceBatch.Exporter);
        Assert.NotNull(traceBatch.Exporter.OtlpHttp);
        var traceExporter = traceBatch.Exporter.OtlpHttp;
        Assert.NotNull(traceExporter);

        Assert.Equal(7000, traceBatch.ScheduleDelay);
        Assert.Equal(35000, traceBatch.ExportTimeout);
        Assert.Equal(4096, traceBatch.MaxQueueSize);
        Assert.Equal(1024, traceBatch.MaxExportBatchSize);

        Assert.Equal("http://collector:4318/v1/traces", traceExporter.Endpoint);
        Assert.Equal(15000, traceBatch.Exporter.OtlpHttp.Timeout);
        Assert.Null(traceExporter.Headers);
        Assert.NotNull(traceExporter.HeadersList);
        Assert.Equal("header1=value1,header2=value2", traceExporter.HeadersList);
    }
}
