// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using Xunit;
using YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased;

[Collection("Non-Parallel Collection")]
public class ParserTests
{
    [Fact]
    public void Parse_FullConfigYaml_ShouldPopulateModelCorrectly()
    {
        var config = Parser.ParseYaml("Configurations/FileBased/Files/TestFile.yaml");

        Assert.NotNull(config);

        Assert.Equal("0.4", config.FileFormat);
        Assert.False(config.Disabled);
        Assert.Equal("info", config.LogLevel);
        Assert.True(config.FailFast);

        Assert.NotNull(config.AttributeLimits);
        Assert.Equal(4096, config.AttributeLimits.AttributeValueLengthLimit);
        Assert.Equal(128, config.AttributeLimits.AttributeCountLimit);

        Assert.NotNull(config.Propagator);
        Assert.Equal("tracecontext,baggage", config.Propagator.CompositeList);

        var composite = config.Propagator.Composite;
        Assert.NotNull(composite);

        string[] expectedCompositeKeys = [
            "tracecontext", "baggage", "b3", "b3multi",
        ];

        foreach (var key in expectedCompositeKeys)
        {
            Assert.True(composite.TryGetValue(key, out var value));
        }

        Assert.NotNull(config.TracerProvider);
        Assert.NotNull(config.TracerProvider.Processors);
        Assert.True(config.TracerProvider.Processors.ContainsKey("batch"));
        var traceBatch = config.TracerProvider.Processors["batch"];
        Assert.NotNull(traceBatch);
        Assert.Equal(5000, traceBatch.ScheduleDelay);
        Assert.Equal(30000, traceBatch.ExportTimeout);
        Assert.Equal(2048, traceBatch.MaxQueueSize);
        Assert.Equal(512, traceBatch.MaxExportBatchSize);
        Assert.NotNull(traceBatch.Exporter);
        Assert.NotNull(traceBatch.Exporter.OtlpHttp);
        var traceExporter = traceBatch.Exporter.OtlpHttp;
        Assert.NotNull(traceExporter);
        Assert.Equal("http://localhost:4318/v1/traces", traceExporter.Endpoint);
        Assert.Equal(10000, traceExporter.Timeout);
        Assert.NotNull(traceExporter.Headers);
        Assert.Empty(traceExporter.Headers);

        Assert.NotNull(traceExporter.HeadersList);
        Assert.Empty(traceExporter.HeadersList);

        Assert.NotNull(config.MeterProvider);
        Assert.NotNull(config.MeterProvider.Readers);
        Assert.True(config.MeterProvider.Readers.ContainsKey("periodic"));
        var metricReader = config.MeterProvider.Readers["periodic"];
        Assert.Equal(60000, metricReader.Interval);
        Assert.Equal(30000, metricReader.Timeout);
        Assert.NotNull(metricReader.Exporter);
        var metricExporter = metricReader.Exporter.OtlpHttp;
        Assert.NotNull(metricExporter);
        Assert.NotNull(metricExporter.Headers);
        Assert.Empty(metricExporter.Headers);
        Assert.Equal("http://localhost:4318/v1/metrics", metricExporter.Endpoint);
        Assert.Equal(10000, metricExporter.Timeout);
        Assert.Empty(metricExporter.Headers);

        Assert.NotNull(config.LoggerProvider);
        Assert.NotNull(config.LoggerProvider.Processors);
        Assert.True(config.LoggerProvider.Processors.ContainsKey("batch"));
        var logBatch = config.LoggerProvider.Processors["batch"];
        Assert.Equal(1000, logBatch.ScheduleDelay);
        Assert.Equal(30000, logBatch.ExportTimeout);
        Assert.Equal(2048, logBatch.MaxQueueSize);
        Assert.Equal(512, logBatch.MaxExportBatchSize);
        var logExporter = logBatch.Exporter?.OtlpHttp;
        Assert.NotNull(logExporter);
        Assert.Equal("http://localhost:4318/v1/logs", logExporter.Endpoint);
        Assert.Equal(10000, logExporter.Timeout);
        Assert.NotNull(logExporter.Headers);
        Assert.Empty(logExporter.Headers);

        Assert.NotNull(config.Resource);
        Assert.NotNull(config.Resource.Attributes);
        Assert.Single(config.Resource.Attributes);

        var serviceAttr = config.Resource.Attributes.First();
        Assert.Equal("service.name", serviceAttr.Name);
        Assert.Equal("unknown_service", serviceAttr.Value);

        Assert.NotNull(config.Resource.AttributesList);
        Assert.Empty(config.Resource.AttributesList);

        Assert.NotNull(config.Resource.DetectionDevelopment);

#if NET
        string[] expectedDetecors = [
            "azureappservice", "container", "host", "operatingsystem", "process", "processruntime"
                ];
#endif
#if NETFRAMEWORK
        string[] expectedDetecors = [
            "azureappservice", "host", "operatingsystem", "process", "processruntime"
                ];
#endif

        var detectors = config.Resource.DetectionDevelopment.Detectors;
        Assert.NotNull(detectors);

        foreach (var alias in expectedDetecors)
        {
            AssertAliasPropertyExists(detectors, alias);
        }

        Assert.NotNull(config.InstrumentationDevelopment);
        Assert.NotNull(config.InstrumentationDevelopment.DotNet);

#if NET
        string[] expectedTraces = [
                    "aspnetcore", "azure", "elasticsearch", "elastictransport",
                    "entityframeworkcore", "graphql", "grpcnetclient", "httpclient",
                    "kafka", "masstransit", "mongodb", "mysqlconnector",
                    "mysqldata", "npgsql", "nservicebus", "oraclemda", "rabbitmq",
                    "quartz", "sqlclient", "stackexchangeredis", "wcfclient"
                ];
#endif
#if NETFRAMEWORK
        string[] expectedTraces = [
                    "aspnet", "azure", "elasticsearch", "elastictransport",
                    "grpcnetclient", "httpclient", "kafka", "mongodb",
                    "mysqlconnector", "npgsql", "nservicebus", "oraclemda",
                    "rabbitmq", "quartz", "sqlclient", "wcfclient", "wcfservice"
                ];
#endif

        var traces = config.InstrumentationDevelopment.DotNet.Traces;
        Assert.NotNull(traces);

        foreach (var alias in expectedTraces)
        {
            AssertAliasPropertyExists(traces, alias);
        }

#if NET
        string[] expectedMetrics =
        [
            "aspnetcore", "httpclient", "netruntime",
            "nservicebus", "process", "sqlclient"
        ];
#endif
#if NETFRAMEWORK
        string[] expectedMetrics =
        [
            "aspnet", "httpclient", "netruntime",
            "nservicebus", "process", "sqlclient"
        ];
#endif

        var metrics = config.InstrumentationDevelopment.DotNet.Metrics;
        Assert.NotNull(metrics);

        foreach (var alias in expectedMetrics)
        {
            AssertAliasPropertyExists(metrics, alias);
        }

        string[] expectedLogs = ["ilogger", "log4net"];

        var logs = config.InstrumentationDevelopment.DotNet.Logs;
        Assert.NotNull(logs);

        foreach (var alias in expectedLogs)
        {
            AssertAliasPropertyExists(logs, alias);
        }
    }

    [Fact]
    public void Parse_EnvVarYaml_ShouldPopulateModelCompletely()
    {
        Environment.SetEnvironmentVariable("OTEL_SDK_DISABLED", "true");
        Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", "debug");
        Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "my‑service");
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "key=value");

        Environment.SetEnvironmentVariable("OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT", "256");
        Environment.SetEnvironmentVariable("OTEL_ATTRIBUTE_COUNT_LIMIT", "256");
        Environment.SetEnvironmentVariable("OTEL_PROPAGATORS", "tracecontext,baggage");

        Environment.SetEnvironmentVariable("OTEL_BSP_SCHEDULE_DELAY", "7000");
        Environment.SetEnvironmentVariable("OTEL_BSP_EXPORT_TIMEOUT", "35000");
        Environment.SetEnvironmentVariable("OTEL_BSP_MAX_QUEUE_SIZE", "4096");
        Environment.SetEnvironmentVariable("OTEL_BSP_MAX_EXPORT_BATCH_SIZE", "1024");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "http://collector:4318/v1/traces");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_TIMEOUT", "15000");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_HEADERS", "header1=value1,header2=value2");

        Environment.SetEnvironmentVariable("OTEL_METRIC_EXPORT_INTERVAL", "65000");
        Environment.SetEnvironmentVariable("OTEL_METRIC_EXPORT_TIMEOUT", "35000");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT", "http://collector:4318/v1/metrics");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_METRICS_TIMEOUT", "15000");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_METRICS_HEADERS", "mheader=value");

        Environment.SetEnvironmentVariable("OTEL_BLRP_SCHEDULE_DELAY", "1500");
        Environment.SetEnvironmentVariable("OTEL_BLRP_EXPORT_TIMEOUT", "40000");
        Environment.SetEnvironmentVariable("OTEL_BLRP_MAX_QUEUE_SIZE", "4096");
        Environment.SetEnvironmentVariable("OTEL_BLRP_MAX_EXPORT_BATCH_SIZE", "1024");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", "http://collector:4318/v1/logs");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_TIMEOUT", "15000");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_HEADERS", "lheader=value");

        var config = Parser.ParseYaml("Configurations/FileBased/Files/TestFileEnvVars.yaml");

        Assert.Equal("0.4", config.FileFormat);
        Assert.True(config.Disabled);
        Assert.Equal("debug", config.LogLevel);

        Assert.NotNull(config.AttributeLimits);
        Assert.Equal(256, config.AttributeLimits.AttributeValueLengthLimit);
        Assert.Equal(256, config.AttributeLimits.AttributeCountLimit);

        Assert.NotNull(config.Propagator);
        Assert.Equal("tracecontext,baggage", config.Propagator.CompositeList);

        Assert.NotNull(config.TracerProvider);
        Assert.NotNull(config.TracerProvider.Processors);
        Assert.True(config.TracerProvider.Processors.ContainsKey("batch"));
        var traceBatch = config.TracerProvider.Processors["batch"];
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

        Assert.NotNull(config.MeterProvider);
        Assert.NotNull(config.MeterProvider.Readers);
        Assert.True(config.MeterProvider.Readers.ContainsKey("periodic"));
        var metricReader = config.MeterProvider.Readers["periodic"];
        Assert.Equal(65000, metricReader.Interval);
        Assert.Equal(35000, metricReader.Timeout);
        Assert.NotNull(metricReader.Exporter);
        var metricExporter = metricReader.Exporter.OtlpHttp;
        Assert.NotNull(metricExporter);
        Assert.Equal("http://collector:4318/v1/metrics", metricExporter.Endpoint);

        Assert.NotNull(config.LoggerProvider);
        Assert.NotNull(config.LoggerProvider.Processors);
        Assert.True(config.LoggerProvider.Processors.ContainsKey("batch"));
        var logBatch = config.LoggerProvider.Processors["batch"];
        Assert.Equal(1500, logBatch.ScheduleDelay);
        Assert.Equal(40000, logBatch.ExportTimeout);
        Assert.Equal(4096, logBatch.MaxQueueSize);
        Assert.Equal(1024, logBatch.MaxExportBatchSize);
        Assert.NotNull(logBatch);
        Assert.NotNull(logBatch.Exporter);
        Assert.NotNull(logBatch.Exporter.OtlpHttp);
        var logExporter = logBatch.Exporter.OtlpHttp;
        Assert.Equal("http://collector:4318/v1/logs", logExporter.Endpoint);
        var serviceAttr = config.Resource?.Attributes?.First(a => a.Name == "service.name");
        Assert.NotNull(serviceAttr);
        Assert.Equal("my‑service", serviceAttr.Value);
    }

    private static void AssertAliasPropertyExists<T>(T obj, string alias)
    {
        Assert.NotNull(obj);

        var prop = typeof(T).GetProperties()
                            .FirstOrDefault(p => p.GetCustomAttribute<YamlMemberAttribute>()?.Alias == alias);

        Assert.NotNull(prop);

        var value = prop.GetValue(obj);
        Assert.NotNull(value);
    }
}
