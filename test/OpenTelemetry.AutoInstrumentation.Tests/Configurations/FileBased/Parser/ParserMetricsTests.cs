// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Xunit;
using YamlParser = OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser.Parser;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased.Parser;

[Collection("Non-Parallel Collection")]
public class ParserMetricsTests
{
    [Fact]
    public void Parse_FullConfigYaml_ShouldPopulateModelCorrectly()
    {
        var config = YamlParser.ParseYaml("Configurations/FileBased/Files/TestMetricsFile.yaml");
        Assert.NotNull(config);

        Assert.NotNull(config.MeterProvider);
        Assert.NotNull(config.MeterProvider!.Readers);
        Assert.Equal(4, config.MeterProvider.Readers.Count);

        var firstReader = config.MeterProvider.Readers[0].Periodic;
        Assert.NotNull(firstReader);
        Assert.Equal(45000, firstReader.Interval);
        Assert.Equal(15000, firstReader.Timeout);
        Assert.NotNull(firstReader.Exporter);
        var firstExporter = firstReader.Exporter!.OtlpHttp;
        Assert.NotNull(firstExporter);
        Assert.Equal("http://localhost:4318/v1/metrics", firstExporter!.Endpoint);
        Assert.Equal(10000, firstExporter.Timeout);
        Assert.Equal("cumulative", firstExporter.TemporalityPreference);

        var secondReader = config.MeterProvider.Readers[1].Periodic;
        Assert.NotNull(secondReader);
        Assert.NotNull(secondReader.Exporter);
        var grpcExporter = secondReader.Exporter!.OtlpGrpc;
        Assert.NotNull(grpcExporter);
        Assert.Equal("http://localhost:4317", grpcExporter!.Endpoint);
        Assert.Equal(8000, grpcExporter.Timeout);
        Assert.Equal("delta", grpcExporter.TemporalityPreference);

        var consoleReader = config.MeterProvider.Readers[2].Periodic;
        Assert.NotNull(consoleReader);
        Assert.NotNull(consoleReader.Exporter);
        Assert.NotNull(consoleReader.Exporter!.Console);

        var prometheusReader = config.MeterProvider.Readers[3].Pull;
        Assert.NotNull(prometheusReader);
        Assert.NotNull(prometheusReader.Exporter);
        Assert.NotNull(prometheusReader.Exporter!.Prometheus);
    }

    [Fact]
    public void Parse_EnvVarYaml_ShouldPopulateModelCompletely()
    {
        Environment.SetEnvironmentVariable("OTEL_METRIC_EXPORT_INTERVAL", "70000");
        Environment.SetEnvironmentVariable("OTEL_METRIC_EXPORT_TIMEOUT", "35000");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT", "http://collector:4318/v1/metrics");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_METRICS_TIMEOUT", "15000");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_METRICS_HEADERS", "header1=value1,header2=value2");

        var config = YamlParser.ParseYaml("Configurations/FileBased/Files/TestMetricsFileEnvVars.yaml");
        Assert.NotNull(config);

        Assert.NotNull(config.MeterProvider);
        Assert.NotNull(config.MeterProvider.Readers);
        var firstReader = config.MeterProvider.Readers[0].Periodic;
        Assert.NotNull(firstReader);
        Assert.Equal(70000, firstReader.Interval);
        Assert.Equal(35000, firstReader.Timeout);
        Assert.NotNull(firstReader.Exporter);
        var exporter = firstReader.Exporter!.OtlpHttp;
        Assert.NotNull(exporter);
        Assert.Equal("http://collector:4318/v1/metrics", exporter.Endpoint);
        Assert.Equal(15000, exporter.Timeout);
        Assert.Null(exporter.Headers);
        Assert.Equal("header1=value1,header2=value2", exporter.HeadersList);

        var secondReader = config.MeterProvider.Readers[1].Periodic;
        Assert.NotNull(secondReader);
        Assert.NotNull(secondReader.Exporter);
        Assert.NotNull(secondReader.Exporter!.Console);
    }
}
