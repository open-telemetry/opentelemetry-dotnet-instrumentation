// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.Exporter;
using Xunit;

using AutoOtlpDefinitions = OpenTelemetry.AutoInstrumentation.Configurations.Otlp.OtlpSpecConfigDefinitions;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

// use collection to indicate that tests should not be run
// in parallel
// see https://xunit.net/docs/running-tests-in-parallel
[Collection("Non-Parallel Collection")]
public class SettingsTests : IDisposable
{
    public SettingsTests()
    {
        ClearEnvVars();
    }

    public void Dispose()
    {
        ClearEnvVars();
    }

    [Fact]
    internal void GeneralSettings_DefaultValues()
    {
        var settings = Settings.FromDefaultSources<GeneralSettings>(false);

        Assert.False(settings.FlushOnUnhandledException);
    }

    [Fact]
    internal void PluginsSettings_DefaultValues()
    {
        var settings = Settings.FromDefaultSources<PluginsSettings>(false);

        Assert.Empty(settings.Plugins);
    }

    [Fact]
    internal void ResourceSettings_DefaultValues()
    {
        var settings = Settings.FromDefaultSources<ResourceSettings>(false);

        Assert.NotEmpty(settings.EnabledDetectors);
        Assert.True(settings.EnvironmentalVariablesDetectorEnabled);
        Assert.Empty(settings.Resources);
    }

    [Fact]
    internal void TracerSettings_DefaultValues()
    {
        var settings = Settings.FromDefaultSources<TracerSettings>(false);

        Assert.True(settings.TracesEnabled);
        var tracesExporter = Assert.Single(settings.TracesExporters);
        Assert.Equal(TracesExporter.Otlp, tracesExporter);
        Assert.NotEmpty(settings.EnabledInstrumentations);
        Assert.Equal(["OpenTelemetry.AutoInstrumentation.*"], settings.ActivitySources);
        Assert.Empty(settings.AdditionalLegacySources);

        // Instrumentation options tests
#if NETFRAMEWORK
        Assert.Empty(settings.InstrumentationOptions.AspNetInstrumentationCaptureRequestHeaders);
        Assert.Empty(settings.InstrumentationOptions.AspNetInstrumentationCaptureResponseHeaders);
#endif
#if NET
        Assert.Empty(settings.InstrumentationOptions.AspNetCoreInstrumentationCaptureRequestHeaders);
        Assert.Empty(settings.InstrumentationOptions.AspNetCoreInstrumentationCaptureResponseHeaders);
        Assert.False(settings.InstrumentationOptions.GraphQLSetDocument);
#endif
        Assert.Empty(settings.InstrumentationOptions.GrpcNetClientInstrumentationCaptureRequestMetadata);
        Assert.Empty(settings.InstrumentationOptions.GrpcNetClientInstrumentationCaptureResponseMetadata);
        Assert.Empty(settings.InstrumentationOptions.HttpInstrumentationCaptureRequestHeaders);
        Assert.Empty(settings.InstrumentationOptions.HttpInstrumentationCaptureResponseHeaders);
        Assert.False(settings.InstrumentationOptions.OracleMdaSetDbStatementForText);

        Assert.NotNull(settings.OtlpSettings);
        Assert.Equal(OtlpExportProtocol.HttpProtobuf, settings.OtlpSettings.Protocol);
        Assert.Null(settings.OtlpSettings.Endpoint);
        Assert.Null(settings.OtlpSettings.Headers);
        Assert.Null(settings.OtlpSettings.TimeoutMilliseconds);
    }

    [Fact]
    internal void MeterSettings_DefaultValues()
    {
        var settings = Settings.FromDefaultSources<MetricSettings>(false);

        Assert.True(settings.MetricsEnabled);
        var metricsExporter = Assert.Single(settings.MetricExporters);
        Assert.Equal(MetricsExporter.Otlp, metricsExporter);
        Assert.NotEmpty(settings.EnabledInstrumentations);
        Assert.Empty(settings.Meters);

        Assert.NotNull(settings.OtlpSettings);
        Assert.Equal(OtlpExportProtocol.HttpProtobuf, settings.OtlpSettings.Protocol);
        Assert.Null(settings.OtlpSettings.Endpoint);
        Assert.Null(settings.OtlpSettings.Headers);
        Assert.Null(settings.OtlpSettings.TimeoutMilliseconds);
    }

    [Fact]
    internal void LogSettings_DefaultValues()
    {
        var settings = Settings.FromDefaultSources<LogSettings>(false);

        Assert.True(settings.LogsEnabled);
        var logExporter = Assert.Single(settings.LogExporters);
        Assert.Equal(LogExporter.Otlp, logExporter);
        Assert.NotEmpty(settings.EnabledInstrumentations);
        Assert.False(settings.IncludeFormattedMessage);

        Assert.NotNull(settings.OtlpSettings);
        Assert.Equal(OtlpExportProtocol.HttpProtobuf, settings.OtlpSettings.Protocol);
        Assert.Null(settings.OtlpSettings.Endpoint);
        Assert.Null(settings.OtlpSettings.Headers);
        Assert.Null(settings.OtlpSettings.TimeoutMilliseconds);
    }

    [Fact]
    internal void SdkSettings_DefaultValues()
    {
        var settings = Settings.FromDefaultSources<SdkSettings>(false);

        Assert.Empty(settings.Propagators);
    }

    [Theory]
    [InlineData("", new[] { TracesExporter.Otlp })]
    [InlineData("none", new TracesExporter[0])]
    [InlineData("otlp", new[] { TracesExporter.Otlp })]
    [InlineData("console", new[] { TracesExporter.Console })]
    [InlineData("zipkin", new[] { TracesExporter.Zipkin })]
    [InlineData("otlp,zipkin,console", new[] { TracesExporter.Otlp, TracesExporter.Zipkin, TracesExporter.Console })]
    [InlineData("none,zipkin", new[] { TracesExporter.Zipkin })]
    [InlineData("otlp,none", new[] { TracesExporter.Otlp })]
    [InlineData("non-supported", new TracesExporter[0])]
    [InlineData("non-supported,none", new TracesExporter[0])]
    [InlineData("zipkin,non-supported,none", new[] { TracesExporter.Zipkin })]
    [InlineData("otlp,otlp", new[] { TracesExporter.Otlp })]
    [InlineData("otlp, ,console", new[] { TracesExporter.Otlp, TracesExporter.Console })]
    [InlineData("otlp, , console", new[] { TracesExporter.Otlp })]
    [InlineData("otlp,,,", new[] { TracesExporter.Otlp })]
    internal void TracesExporters(string tracesExporters, TracesExporter[] expectedTracesExporters)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.Exporter, tracesExporters);

        var settings = Settings.FromDefaultSources<TracerSettings>(false);

        Assert.Equal(expectedTracesExporters, settings.TracesExporters);
    }

    [Theory]
    [InlineData("non-supported")]
    [InlineData("otlp,otlp")]
    [InlineData("otlp, ,console")]
    [InlineData("otlp, console")]
    [InlineData("otlp,,,")]
    internal void TracesExporters_FailFast(string tracesExporters)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.Exporter, tracesExporters);

        Assert.Throws<NotSupportedException>(() => Settings.FromDefaultSources<TracerSettings>(true));
    }

    [Theory]
    [InlineData("", new[] { MetricsExporter.Otlp })]
    [InlineData("none", new MetricsExporter[0])]
    [InlineData("otlp", new[] { MetricsExporter.Otlp })]
    [InlineData("prometheus", new[] { MetricsExporter.Prometheus })]
    [InlineData("console", new[] { MetricsExporter.Console })]
    [InlineData("otlp,prometheus,console", new[] { MetricsExporter.Otlp, MetricsExporter.Prometheus, MetricsExporter.Console })]
    [InlineData("prometheus,none", new[] { MetricsExporter.Prometheus })]
    [InlineData("non-supported", new MetricsExporter[0])]
    [InlineData("non-supported,none", new MetricsExporter[0])]
    [InlineData("prometheus,non-supported,none", new[] { MetricsExporter.Prometheus })]
    [InlineData("otlp,non-supported,none", new[] { MetricsExporter.Otlp })]
    [InlineData("otlp,otlp", new[] { MetricsExporter.Otlp })]
    [InlineData("otlp, ,console", new[] { MetricsExporter.Otlp, MetricsExporter.Console })]
    [InlineData("otlp, , console", new[] { MetricsExporter.Otlp })]
    [InlineData("otlp,,,", new[] { MetricsExporter.Otlp })]
    internal void MetricExporters(string metricExporters, MetricsExporter[] expectedMetricsExporters)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.Exporter, metricExporters);

        var settings = Settings.FromDefaultSources<MetricSettings>(false);

        Assert.Equal(expectedMetricsExporters, settings.MetricExporters);
    }

    [Theory]
    [InlineData("non-supported")]
    [InlineData("otlp,otlp")]
    [InlineData("otlp, ,console")]
    [InlineData("otlp, console")]
    [InlineData("otlp,,,")]
    internal void MetricExporters_FailFast(string metricExporters)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.Exporter, metricExporters);

        Assert.Throws<NotSupportedException>(() => Settings.FromDefaultSources<MetricSettings>(true));
    }

    [Theory]
    [InlineData("", new[] { LogExporter.Otlp })]
    [InlineData("none", new LogExporter[0])]
    [InlineData("otlp", new[] { LogExporter.Otlp })]
    [InlineData("console", new[] { LogExporter.Console })]
    [InlineData("otlp,none", new[] { LogExporter.Otlp })]
    [InlineData("otlp,console", new[] { LogExporter.Otlp, LogExporter.Console })]
    [InlineData("none,otlp", new[] { LogExporter.Otlp })]
    [InlineData("non-supported", new LogExporter[0])]
    [InlineData("non-supported,none", new LogExporter[0])]
    [InlineData("non-supported,none,otlp", new[] { LogExporter.Otlp })]
    [InlineData("otlp,otlp", new[] { LogExporter.Otlp })]
    [InlineData("otlp, ,console", new[] { LogExporter.Otlp, LogExporter.Console })]
    [InlineData("otlp, console", new[] { LogExporter.Otlp })]
    [InlineData("otlp,,,", new[] { LogExporter.Otlp })]
    internal void LogExporters(string logExporters, LogExporter[] expectedLogExporters)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Logs.Exporter, logExporters);

        var settings = Settings.FromDefaultSources<LogSettings>(false);

        Assert.Equal(expectedLogExporters, settings.LogExporters);
    }

    [Theory]
    [InlineData("non-supported")]
    [InlineData("otlp,otlp")]
    [InlineData("otlp, ,console")]
    [InlineData("otlp, console")]
    [InlineData("otlp,,,")]
    internal void LogExporters_FailFast(string logExporters)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Logs.Exporter, logExporters);

        Assert.Throws<NotSupportedException>(() => Settings.FromDefaultSources<LogSettings>(true));
    }

    [Theory]
    [InlineData(null, new Propagator[0])]
    [InlineData("", new Propagator[0])]
    [InlineData("tracecontext", new[] { Propagator.W3CTraceContext })]
    [InlineData("baggage", new[] { Propagator.W3CBaggage })]
    [InlineData("b3multi", new[] { Propagator.B3Multi })]
    [InlineData("b3", new[] { Propagator.B3Single })]
    [InlineData("tracecontext,baggage,b3multi,b3", new[] { Propagator.W3CTraceContext, Propagator.W3CBaggage, Propagator.B3Multi, Propagator.B3Single })]
    [InlineData("not-supported", new Propagator[0])]
    [InlineData("not-supported1,not-supported2", new Propagator[0])]
    [InlineData("not-supported,b3", new[] { Propagator.B3Single })]
    [InlineData("tracecontext,tracecontext", new[] { Propagator.W3CTraceContext })]
    [InlineData("tracecontext, ,b3multi", new[] { Propagator.W3CTraceContext, Propagator.B3Multi })]
    [InlineData("tracecontext, b3multi", new[] { Propagator.W3CTraceContext })]
    [InlineData("tracecontext,,,", new[] { Propagator.W3CTraceContext })]
    internal void Propagators(string? propagators, Propagator[] expectedPropagators)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Sdk.Propagators, propagators);

        var settings = Settings.FromDefaultSources<SdkSettings>(false);

        Assert.Equal(expectedPropagators, settings.Propagators);
    }

    [Theory]
    [InlineData("not-supported")]
    [InlineData("tracecontext,tracecontext")]
    [InlineData("tracecontext, ,b3multi")]
    [InlineData("tracecontext, baggage")]
    [InlineData("tracecontext,,,")]
    internal void Propagators_FailFast(string propagators)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Sdk.Propagators, propagators);

        Assert.Throws<NotSupportedException>(() => Settings.FromDefaultSources<SdkSettings>(true));
    }

    [Theory]
#if NETFRAMEWORK
    [InlineData("ASPNET", TracerInstrumentation.AspNet)]
#endif
#if NET
    [InlineData("GRAPHQL", TracerInstrumentation.GraphQL)]
#endif
    [InlineData("HTTPCLIENT", TracerInstrumentation.HttpClient)]
    [InlineData("MONGODB", TracerInstrumentation.MongoDB)]
#if NET
    [InlineData("MYSQLDATA", TracerInstrumentation.MySqlData)]
    [InlineData("STACKEXCHANGEREDIS", TracerInstrumentation.StackExchangeRedis)]
#endif
    [InlineData("NPGSQL", TracerInstrumentation.Npgsql)]
    [InlineData("SQLCLIENT", TracerInstrumentation.SqlClient)]
    [InlineData("GRPCNETCLIENT", TracerInstrumentation.GrpcNetClient)]
#if NETFRAMEWORK
    [InlineData("WCFSERVICE", TracerInstrumentation.WcfService)]
#endif
#if NET
    [InlineData("MASSTRANSIT", TracerInstrumentation.MassTransit)]
#endif
    [InlineData("NSERVICEBUS", TracerInstrumentation.NServiceBus)]
    [InlineData("ELASTICSEARCH", TracerInstrumentation.Elasticsearch)]
    [InlineData("QUARTZ", TracerInstrumentation.Quartz)]
#if NET
    [InlineData("ENTITYFRAMEWORKCORE", TracerInstrumentation.EntityFrameworkCore)]
    [InlineData("ASPNETCORE", TracerInstrumentation.AspNetCore)]
#endif
    [InlineData("WCFCLIENT", TracerInstrumentation.WcfClient)]
    [InlineData("MYSQLCONNECTOR", TracerInstrumentation.MySqlConnector)]
    [InlineData("AZURE", TracerInstrumentation.Azure)]
    [InlineData("ELASTICTRANSPORT", TracerInstrumentation.ElasticTransport)]
    [InlineData("KAFKA", TracerInstrumentation.Kafka)]
    [InlineData("ORACLEMDA", TracerInstrumentation.OracleMda)]
    [InlineData("RABBITMQ", TracerInstrumentation.RabbitMq)]
    internal void TracerSettings_Instrumentations_SupportedValues(string tracerInstrumentation, TracerInstrumentation expectedTracerInstrumentation)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.TracesInstrumentationEnabled, "false");
        Environment.SetEnvironmentVariable(string.Format(ConfigurationKeys.Traces.EnabledTracesInstrumentationTemplate, tracerInstrumentation), "true");

        var settings = Settings.FromDefaultSources<TracerSettings>(false);

        Assert.Equal([expectedTracerInstrumentation], settings.EnabledInstrumentations);
    }

    [Theory]
#if NETFRAMEWORK
    [InlineData("ASPNET", MetricInstrumentation.AspNet)]
#endif
    [InlineData("NETRUNTIME", MetricInstrumentation.NetRuntime)]
    [InlineData("HTTPCLIENT", MetricInstrumentation.HttpClient)]
    [InlineData("PROCESS", MetricInstrumentation.Process)]
#if NET
    [InlineData("NPGSQL", MetricInstrumentation.Npgsql)]
#endif
    [InlineData("NSERVICEBUS", MetricInstrumentation.NServiceBus)]
#if NET
    [InlineData("ASPNETCORE", MetricInstrumentation.AspNetCore)]
#endif
    [InlineData("SQLCLIENT", MetricInstrumentation.SqlClient)]
    internal void MeterSettings_Instrumentations_SupportedValues(string meterInstrumentation, MetricInstrumentation expectedMetricInstrumentation)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.MetricsInstrumentationEnabled, "false");
        Environment.SetEnvironmentVariable(string.Format(ConfigurationKeys.Metrics.EnabledMetricsInstrumentationTemplate, meterInstrumentation), "true");

        var settings = Settings.FromDefaultSources<MetricSettings>(false);

        Assert.Equal([expectedMetricInstrumentation], settings.EnabledInstrumentations);
    }

    [Theory]
    [InlineData("ILOGGER", LogInstrumentation.ILogger)]
    [InlineData("LOG4NET", LogInstrumentation.Log4Net)]
    internal void LogSettings_Instrumentations_SupportedValues(string logInstrumentation, LogInstrumentation expectedLogInstrumentation)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Logs.LogsInstrumentationEnabled, "false");
        Environment.SetEnvironmentVariable(string.Format(ConfigurationKeys.Logs.EnabledLogsInstrumentationTemplate, logInstrumentation), "true");

        var settings = Settings.FromDefaultSources<LogSettings>(false);

        Assert.Equal([expectedLogInstrumentation], settings.EnabledInstrumentations);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    internal void IncludeFormattedMessage_DependsOnCorrespondingEnvVariable(string includeFormattedMessage, bool expectedValue)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Logs.IncludeFormattedMessage, includeFormattedMessage);

        var settings = Settings.FromDefaultSources<LogSettings>(false);

        Assert.Equal(expectedValue, settings.IncludeFormattedMessage);
    }

    [Theory]
    [InlineData("", OtlpExportProtocol.HttpProtobuf)]
    [InlineData(null, OtlpExportProtocol.HttpProtobuf)]
    [InlineData("http/protobuf", OtlpExportProtocol.HttpProtobuf)]
#if NETFRAMEWORK
    [InlineData("grpc", OtlpExportProtocol.HttpProtobuf)]
#else
    [InlineData("grpc", OtlpExportProtocol.Grpc)]
#endif
    [InlineData("nonExistingProtocol", OtlpExportProtocol.HttpProtobuf)]
    internal void OtlpExportProtocol_DependsOnCorrespondingEnvVariable(string? otlpProtocol, OtlpExportProtocol? expectedOtlpExportProtocol)
    {
        Environment.SetEnvironmentVariable(AutoOtlpDefinitions.DefaultProtocolEnvVarName, otlpProtocol);

        var settings = Settings.FromDefaultSources<TracerSettings>(false);

        // null values for expected data will be handled by OTel .NET SDK
        Assert.NotNull(settings.OtlpSettings);
        Assert.Equal(expectedOtlpExportProtocol, settings.OtlpSettings.Protocol);
    }

    [Theory]
    // endpoint, protocol, timeout, headers, expectedProtocol, expectedTimeout
    [InlineData("http://example.org/traces/v1", "http/protobuf", "1", "key1=value1,key2=value2", OtlpExportProtocol.HttpProtobuf, 1)]
#if NETFRAMEWORK
    [InlineData("http://example.org/traces/v1", "grpc", "15000", "a=b,c=d", OtlpExportProtocol.HttpProtobuf, 15000)]
#else
    [InlineData("http://example.org/traces/v1", "grpc", "15000", "a=b,c=d", OtlpExportProtocol.Grpc, 15000)]
#endif
    [InlineData("http://example.org/traces/v1", "invalid", "42", "x=y", OtlpExportProtocol.HttpProtobuf, 42)]
    internal void OtlpExportProtocol_CheckPriorityEnvIsSet_Traces(string endpoint, string protocol, string timeout, string headers, OtlpExportProtocol? expectedProtocol, int expectedTimeout)
    {
        Environment.SetEnvironmentVariable(AutoOtlpDefinitions.TracesEndpointEnvVarName, endpoint);
        Environment.SetEnvironmentVariable(AutoOtlpDefinitions.TracesProtocolEnvVarName, protocol);
        Environment.SetEnvironmentVariable(AutoOtlpDefinitions.TracesTimeoutEnvVarName, timeout);
        Environment.SetEnvironmentVariable(AutoOtlpDefinitions.TracesHeadersEnvVarName, headers);

        var settings = Settings.FromDefaultSources<TracerSettings>(false).OtlpSettings;

        Assert.NotNull(settings);
        Assert.Equal(new Uri(endpoint), settings.Endpoint);
        Assert.Equal(expectedProtocol, settings.Protocol);
        Assert.Equal(expectedTimeout, settings.TimeoutMilliseconds);
        Assert.Equal(headers, settings.Headers);
    }

    [Theory]
    [InlineData("http://example.org/metrics/v1", "http/protobuf", "1", "key1=value1,key2=value2", OtlpExportProtocol.HttpProtobuf, 1)]
#if NETFRAMEWORK
    [InlineData("http://example.org/metrics/v1", "grpc", "25000", "m=n", OtlpExportProtocol.HttpProtobuf, 25000)]
#else
    [InlineData("http://example.org/metrics/v1", "grpc", "25000", "m=n", OtlpExportProtocol.Grpc, 25000)]
#endif
    [InlineData("http://example.org/metrics/v1", "invalid", "100", "a=b", OtlpExportProtocol.HttpProtobuf, 100)]
    internal void OtlpExportProtocol_CheckPriorityEnvIsSet_Metrics(string endpoint, string protocol, string timeout, string headers, OtlpExportProtocol? expectedProtocol, int expectedTimeout)
    {
        Environment.SetEnvironmentVariable(AutoOtlpDefinitions.MetricsEndpointEnvVarName, endpoint);
        Environment.SetEnvironmentVariable(AutoOtlpDefinitions.MetricsProtocolEnvVarName, protocol);
        Environment.SetEnvironmentVariable(AutoOtlpDefinitions.MetricsTimeoutEnvVarName, timeout);
        Environment.SetEnvironmentVariable(AutoOtlpDefinitions.MetricsHeadersEnvVarName, headers);

        var settings = Settings.FromDefaultSources<MetricSettings>(false).OtlpSettings;

        Assert.NotNull(settings);
        Assert.Equal(new Uri(endpoint), settings.Endpoint);
        Assert.Equal(expectedProtocol, settings.Protocol);
        Assert.Equal(expectedTimeout, settings.TimeoutMilliseconds);
        Assert.Equal(headers, settings.Headers);
    }

    [Theory]
    [InlineData("http://example.org/logs/v1", "http/protobuf", "1", "key1=value1,key2=value2", OtlpExportProtocol.HttpProtobuf, 1)]
#if NETFRAMEWORK
    [InlineData("http://example.org/logs/v1", "grpc", "5000", "l=m", OtlpExportProtocol.HttpProtobuf, 5000)]
#else
    [InlineData("http://example.org/logs/v1", "grpc", "5000", "l=m", OtlpExportProtocol.Grpc, 5000)]
#endif
    [InlineData("http://example.org/logs/v1", "invalid", "77", "z=zz", OtlpExportProtocol.HttpProtobuf, 77)]
    internal void OtlpExportProtocol_CheckPriorityEnvIsSet_Logs(string endpoint, string protocol, string timeout, string headers, OtlpExportProtocol? expectedProtocol, int expectedTimeout)
    {
        Environment.SetEnvironmentVariable(AutoOtlpDefinitions.LogsEndpointEnvVarName, endpoint);
        Environment.SetEnvironmentVariable(AutoOtlpDefinitions.LogsProtocolEnvVarName, protocol);
        Environment.SetEnvironmentVariable(AutoOtlpDefinitions.LogsTimeoutEnvVarName, timeout);
        Environment.SetEnvironmentVariable(AutoOtlpDefinitions.LogsHeadersEnvVarName, headers);

        var settings = Settings.FromDefaultSources<LogSettings>(false).OtlpSettings;

        Assert.NotNull(settings);
        Assert.Equal(new Uri(endpoint), settings.Endpoint);
        Assert.Equal(expectedProtocol, settings.Protocol);
        Assert.Equal(expectedTimeout, settings.TimeoutMilliseconds);
        Assert.Equal(headers, settings.Headers);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData(null, false)]
    internal void FlushOnUnhandledException_DependsOnCorrespondingEnvVariable(string? flushOnUnhandledException, bool expectedValue)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.FlushOnUnhandledException, flushOnUnhandledException);

        var settings = Settings.FromDefaultSources<GeneralSettings>(false);

        Assert.Equal(expectedValue, settings.FlushOnUnhandledException);
    }

    [Theory]
#if NET
    [InlineData("CONTAINER", ResourceDetector.Container)]
#endif
    [InlineData("AZUREAPPSERVICE", ResourceDetector.AzureAppService)]
    [InlineData("PROCESSRUNTIME", ResourceDetector.ProcessRuntime)]
    [InlineData("PROCESS", ResourceDetector.Process)]
    [InlineData("OPERATINGSYSTEM", ResourceDetector.OperatingSystem)]
    [InlineData("HOST", ResourceDetector.Host)]
    internal void ResourceSettings_ResourceDetectors_SupportedValues(string resourceDetector, ResourceDetector expectedResourceDetector)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.ResourceDetectorEnabled, "false");
        Environment.SetEnvironmentVariable(string.Format(ConfigurationKeys.EnabledResourceDetectorTemplate, resourceDetector), "true");

        var settings = Settings.FromDefaultSources<ResourceSettings>(false);

        Assert.Equal([expectedResourceDetector], settings.EnabledDetectors);
    }

    private static void ClearEnvVars()
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Logs.LogsInstrumentationEnabled, null);
        foreach (var logInstrumentation in Enum.GetValues(typeof(LogInstrumentation)).Cast<LogInstrumentation>())
        {
            Environment.SetEnvironmentVariable(string.Format(ConfigurationKeys.Logs.EnabledLogsInstrumentationTemplate, logInstrumentation.ToString().ToUpperInvariant()), null);
        }

        Environment.SetEnvironmentVariable(ConfigurationKeys.Logs.Exporter, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Logs.IncludeFormattedMessage, null);

        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.MetricsInstrumentationEnabled, null);
        foreach (var metricInstrumentation in Enum.GetValues(typeof(MetricInstrumentation)).Cast<MetricInstrumentation>())
        {
            Environment.SetEnvironmentVariable(string.Format(ConfigurationKeys.Metrics.EnabledMetricsInstrumentationTemplate, metricInstrumentation.ToString().ToUpperInvariant()), null);
        }

        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.Exporter, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.TracesInstrumentationEnabled, null);
        foreach (var tracerInstrumentation in Enum.GetValues(typeof(TracerInstrumentation)).Cast<TracerInstrumentation>())
        {
            Environment.SetEnvironmentVariable(string.Format(ConfigurationKeys.Traces.EnabledTracesInstrumentationTemplate, tracerInstrumentation.ToString().ToUpperInvariant()), null);
        }

        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.Exporter, null);
        Environment.SetEnvironmentVariable(AutoOtlpDefinitions.DefaultProtocolEnvVarName, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.FlushOnUnhandledException, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.ResourceDetectorEnabled, null);

        Environment.SetEnvironmentVariable(ConfigurationKeys.ResourceDetectorEnabled, null);
        foreach (var resourceDetector in Enum.GetValues(typeof(ResourceDetector)).Cast<ResourceDetector>())
        {
            Environment.SetEnvironmentVariable(string.Format(ConfigurationKeys.EnabledResourceDetectorTemplate, resourceDetector.ToString().ToUpperInvariant()), null);
        }

        Environment.SetEnvironmentVariable(ConfigurationKeys.Sdk.Propagators, null);

#if NETFRAMEWORK
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.InstrumentationOptions.AspNetInstrumentationCaptureRequestHeaders, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.InstrumentationOptions.AspNetInstrumentationCaptureResponseHeaders, null);
#endif

#if NET
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.InstrumentationOptions.AspNetCoreInstrumentationCaptureRequestHeaders, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.InstrumentationOptions.AspNetCoreInstrumentationCaptureResponseHeaders, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.InstrumentationOptions.GraphQLSetDocument, null);
#endif
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.InstrumentationOptions.GrpcNetClientInstrumentationCaptureRequestMetadata, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.InstrumentationOptions.GrpcNetClientInstrumentationCaptureResponseMetadata, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.InstrumentationOptions.HttpInstrumentationCaptureRequestHeaders, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.InstrumentationOptions.HttpInstrumentationCaptureResponseHeaders, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.InstrumentationOptions.OracleMdaSetDbStatementForText, null);

        // Cleanup OTLP env vars
        foreach (var envVar in GetAllOtlpEnvVarNames())
        {
            Environment.SetEnvironmentVariable(envVar, null);
        }
    }

    private static IEnumerable<string> GetAllOtlpEnvVarNames()
    {
        return typeof(AutoOtlpDefinitions)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToList() ?? Enumerable.Empty<string>();
    }
}
