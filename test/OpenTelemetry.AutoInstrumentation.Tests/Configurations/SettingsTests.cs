// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Specialized;
using System.Globalization;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Tests.Util;
using OpenTelemetry.Exporter;
using Xunit;

using AutoOtlpDefinitions = OpenTelemetry.AutoInstrumentation.Configurations.Otlp.OtlpSpecConfigDefinitions;

#if NETFRAMEWORK
using SystemConfigurationManager = System.Configuration.ConfigurationManager;
#endif

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

// use collection to indicate that tests should not be run
// in parallel
// see https://xunit.net/docs/running-tests-in-parallel
[Collection("Non-Parallel Collection")]
public sealed class SettingsTests
{
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
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { ConfigurationKeys.Traces.Exporter, tracesExporters }
        });

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
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { ConfigurationKeys.Traces.Exporter, tracesExporters }
        });

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
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { ConfigurationKeys.Metrics.Exporter, metricExporters }
        });

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
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { ConfigurationKeys.Metrics.Exporter, metricExporters }
        });

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
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { ConfigurationKeys.Logs.Exporter, logExporters }
        });

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
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { ConfigurationKeys.Logs.Exporter, logExporters }
        });

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
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { ConfigurationKeys.Sdk.Propagators, propagators }
        });

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
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { ConfigurationKeys.Sdk.Propagators, propagators }
        });

        Assert.Throws<NotSupportedException>(() => Settings.FromDefaultSources<SdkSettings>(true));
    }

    [Theory]
#if NETFRAMEWORK
    [InlineData("ASPNET", TracerInstrumentation.AspNet)]
#endif
#if NET
    [InlineData("GRAPHQL", TracerInstrumentation.GraphQL)]
#endif
    [InlineData("GRPCNETCLIENT", TracerInstrumentation.GrpcNetClient)]
    [InlineData("HTTPCLIENT", TracerInstrumentation.HttpClient)]
    [InlineData("MONGODB", TracerInstrumentation.MongoDB)]
#if NET
    [InlineData("MYSQLDATA", TracerInstrumentation.MySqlData)]
#endif
    [InlineData("NPGSQL", TracerInstrumentation.Npgsql)]
    [InlineData("SQLCLIENT", TracerInstrumentation.SqlClient)]
    [InlineData("STACKEXCHANGEREDIS", TracerInstrumentation.StackExchangeRedis)]
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
#if NET
    [InlineData("WCFCORE", TracerInstrumentation.WcfCore)]
#endif
    [InlineData("ADONET", TracerInstrumentation.AdoNet)]
    [InlineData("SQLITE", TracerInstrumentation.Sqlite)]
    internal void TracerSettings_Instrumentations_SupportedValues(string tracerInstrumentation, TracerInstrumentation expectedTracerInstrumentation)
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { ConfigurationKeys.Traces.TracesInstrumentationEnabled, "false" },
            { string.Format(CultureInfo.InvariantCulture, ConfigurationKeys.Traces.EnabledTracesInstrumentationTemplate, tracerInstrumentation), "true" },
        });

        var settings = Settings.FromDefaultSources<TracerSettings>(false);

        Assert.Equal([expectedTracerInstrumentation], settings.EnabledInstrumentations);
    }

#if NET
    [Fact]
    internal void TracerSettings_Instrumentations_EntityFrameworkCoreAndNpgsqlCanBeEnabledTogether()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { ConfigurationKeys.Traces.TracesInstrumentationEnabled, "false" },
            { string.Format(CultureInfo.InvariantCulture, ConfigurationKeys.Traces.EnabledTracesInstrumentationTemplate, "ENTITYFRAMEWORKCORE"), "true" },
            { string.Format(CultureInfo.InvariantCulture, ConfigurationKeys.Traces.EnabledTracesInstrumentationTemplate, "NPGSQL"), "true" },
        });

        var settings = Settings.FromDefaultSources<TracerSettings>(false);

        Assert.Contains(TracerInstrumentation.EntityFrameworkCore, settings.EnabledInstrumentations);
        Assert.Contains(TracerInstrumentation.Npgsql, settings.EnabledInstrumentations);
    }
#endif

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
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { ConfigurationKeys.Metrics.MetricsInstrumentationEnabled, "false" },
            { string.Format(CultureInfo.InvariantCulture, ConfigurationKeys.Metrics.EnabledMetricsInstrumentationTemplate, meterInstrumentation), "true" }
        });

        var settings = Settings.FromDefaultSources<MetricSettings>(false);

        Assert.Equal([expectedMetricInstrumentation], settings.EnabledInstrumentations);
    }

    [Theory]
    [InlineData("ILOGGER", LogInstrumentation.ILogger)]
    [InlineData("LOG4NET", LogInstrumentation.Log4Net)]
    [InlineData("NLOG", LogInstrumentation.NLog)]
    internal void LogSettings_Instrumentations_SupportedValues(string logInstrumentation, LogInstrumentation expectedLogInstrumentation)
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { ConfigurationKeys.Logs.LogsInstrumentationEnabled, "false" },
            { string.Format(CultureInfo.InvariantCulture, ConfigurationKeys.Logs.EnabledLogsInstrumentationTemplate, logInstrumentation), "true" }
        });

        var settings = Settings.FromDefaultSources<LogSettings>(false);

        Assert.Equal([expectedLogInstrumentation], settings.EnabledInstrumentations);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    internal void IncludeFormattedMessage_DependsOnCorrespondingEnvVariable(string includeFormattedMessage, bool expectedValue)
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { ConfigurationKeys.Logs.IncludeFormattedMessage, includeFormattedMessage }
        });

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
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { AutoOtlpDefinitions.DefaultProtocolEnvVarName, otlpProtocol }
        });

        var settings = Settings.FromDefaultSources<TracerSettings>(false);

        // null values for expected data will be handled by OTel .NET SDK
        Assert.NotNull(settings.OtlpSettings);
        Assert.Equal(expectedOtlpExportProtocol, settings.OtlpSettings.Protocol);
    }

    [Theory]
    [InlineData("http/protobuf", "1", "key1=value1,key2=value2", OtlpExportProtocol.HttpProtobuf, 1)]
#if NETFRAMEWORK
    [InlineData("grpc", "15000", "a=b,c=d", OtlpExportProtocol.HttpProtobuf, 15000)]
#else
    [InlineData("grpc", "15000", "a=b,c=d", OtlpExportProtocol.Grpc, 15000)]
#endif
    [InlineData("invalid", "42", "x=y", OtlpExportProtocol.HttpProtobuf, 42)]
    internal void OtlpExportProtocol_CheckPriorityEnvIsSet_Traces(string? protocol, string timeout, string headers, OtlpExportProtocol? expectedProtocol, int expectedTimeout)
    {
        var unexpectedProtocolForDistraction = expectedProtocol == OtlpExportProtocol.HttpProtobuf ? "grpc" : "http/protobuf";
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { AutoOtlpDefinitions.DefaultProtocolEnvVarName, unexpectedProtocolForDistraction }, // set a different default to verify priority
            { AutoOtlpDefinitions.TracesProtocolEnvVarName, protocol },
            { AutoOtlpDefinitions.TracesTimeoutEnvVarName, timeout },
            { AutoOtlpDefinitions.TracesHeadersEnvVarName, headers },
        });

        var settings = Settings.FromDefaultSources<TracerSettings>(false).OtlpSettings;

        Assert.NotNull(settings);
        Assert.Equal(expectedProtocol, settings.Protocol);
        Assert.Equal(expectedTimeout, settings.TimeoutMilliseconds);
        Assert.Equal(headers, settings.Headers);
    }

    [Theory]
    [InlineData("http/protobuf", "1", "key1=value1,key2=value2", OtlpExportProtocol.HttpProtobuf, 1)]
#if NETFRAMEWORK
    [InlineData("grpc", "25000", "m=n", OtlpExportProtocol.HttpProtobuf, 25000)]
#else
    [InlineData("grpc", "25000", "m=n", OtlpExportProtocol.Grpc, 25000)]
#endif
    [InlineData("invalid", "100", "a=b", OtlpExportProtocol.HttpProtobuf, 100)]
    internal void OtlpExportProtocol_CheckPriorityEnvIsSet_Metrics(string? protocol, string timeout, string headers, OtlpExportProtocol? expectedProtocol, int expectedTimeout)
    {
        var unexpectedProtocolForDistraction = expectedProtocol == OtlpExportProtocol.HttpProtobuf ? "grpc" : "http/protobuf";
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { AutoOtlpDefinitions.DefaultProtocolEnvVarName, unexpectedProtocolForDistraction }, // set a different default to verify priority
            { AutoOtlpDefinitions.MetricsProtocolEnvVarName, protocol },
            { AutoOtlpDefinitions.MetricsTimeoutEnvVarName, timeout },
            { AutoOtlpDefinitions.MetricsHeadersEnvVarName, headers },
        });

        var settings = Settings.FromDefaultSources<MetricSettings>(false).OtlpSettings;

        Assert.NotNull(settings);
        Assert.Equal(expectedProtocol, settings.Protocol);
        Assert.Equal(expectedTimeout, settings.TimeoutMilliseconds);
        Assert.Equal(headers, settings.Headers);
    }

    [Theory]
    [InlineData("http/protobuf", "1", "key1=value1,key2=value2", OtlpExportProtocol.HttpProtobuf, 1)]
#if NETFRAMEWORK
    [InlineData("grpc", "5000", "l=m", OtlpExportProtocol.HttpProtobuf, 5000)]
#else
    [InlineData("grpc", "5000", "l=m", OtlpExportProtocol.Grpc, 5000)]
#endif
    [InlineData("invalid", "77", "z=zz", OtlpExportProtocol.HttpProtobuf, 77)]
    internal void OtlpExportProtocol_CheckPriorityEnvIsSet_Logs(string? protocol, string timeout, string headers, OtlpExportProtocol? expectedProtocol, int expectedTimeout)
    {
        var unexpectedProtocolForDistraction = expectedProtocol == OtlpExportProtocol.HttpProtobuf ? "grpc" : "http/protobuf";
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { AutoOtlpDefinitions.DefaultProtocolEnvVarName, unexpectedProtocolForDistraction }, // set a different default to verify priority
            { AutoOtlpDefinitions.LogsProtocolEnvVarName, protocol },
            { AutoOtlpDefinitions.LogsTimeoutEnvVarName, timeout },
            { AutoOtlpDefinitions.LogsHeadersEnvVarName, headers },
        });

        var settings = Settings.FromDefaultSources<LogSettings>(false).OtlpSettings;

        Assert.NotNull(settings);
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
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { ConfigurationKeys.FlushOnUnhandledException, flushOnUnhandledException }
        });

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
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { ConfigurationKeys.ResourceDetectorEnabled, "false" },
            { string.Format(CultureInfo.InvariantCulture, ConfigurationKeys.EnabledResourceDetectorTemplate, resourceDetector), "true" },
        });

        var settings = Settings.FromDefaultSources<ResourceSettings>(false);

        Assert.Equal([expectedResourceDetector], settings.EnabledDetectors);
    }

    [Fact]
    internal void ResourceSettings_LoadEnvVar_ReadsServiceNameFromConfigurationSource()
    {
        const string appSettingsValue = "service-name-from-app-settings";

        var configuration = new Configuration(
            false,
            new NameValueConfigurationSource(false, new NameValueCollection
            {
                { ConfigurationKeys.ServiceName, appSettingsValue }
            }));
        var settings = new ResourceSettings();

        settings.LoadEnvVar(configuration);

        var resource = Assert.Single(settings.Resources);
        Assert.Equal(Constants.ResourceAttributes.AttributeServiceName, resource.Key);
        Assert.Equal(appSettingsValue, resource.Value);
    }

    [Fact]
    internal void ResourceSettings_LoadEnvVar_AppSettingsSourceTakesPriorityOverEnvVarSource()
    {
        const string envVarValue = "service-name-from-env-var";
        const string appSettingsValue = "service-name-from-app-settings";

        try
        {
            Environment.SetEnvironmentVariable(ConfigurationKeys.ServiceName, envVarValue);
            var configuration = new Configuration(
                false,
                new NameValueConfigurationSource(false, new NameValueCollection
                {
                    { ConfigurationKeys.ServiceName, appSettingsValue }
                }),
                new EnvironmentConfigurationSource(false));
            var settings = new ResourceSettings();

            settings.LoadEnvVar(configuration);

            var resource = Assert.Single(settings.Resources);
            Assert.Equal(Constants.ResourceAttributes.AttributeServiceName, resource.Key);
            Assert.Equal(appSettingsValue, resource.Value);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConfigurationKeys.ServiceName, null);
        }
    }

    [Fact]
    internal void ResourceSettings_LoadEnvVar_ServiceNameFallsBackToLaterSource()
    {
        var configuration = new OpenTelemetry.AutoInstrumentation.Configurations.Configuration(
            false,
            new NameValueConfigurationSource(false, []),
            new NameValueConfigurationSource(false, new NameValueCollection
            {
                { ConfigurationKeys.ServiceName, "service-name-from-fallback-source" }
            }));
        var settings = new ResourceSettings();

        settings.LoadEnvVar(configuration);

        var resource = Assert.Single(settings.Resources);
        Assert.Equal(Constants.ResourceAttributes.AttributeServiceName, resource.Key);
        Assert.Equal("service-name-from-fallback-source", resource.Value);
    }

    [Fact]
    internal void ResourceSettings_LoadEnvVar_ServiceNameOverridesServiceNameInResourceAttributes()
    {
        var configuration = new Configuration(
            false,
            new NameValueConfigurationSource(false, new NameValueCollection
            {
                { ConfigurationKeys.ResourceAttributes, "service.name=resource-attributes,deployment.environment=prod" },
                { ConfigurationKeys.ServiceName, "service-name-setting" }
            }));
        var settings = new ResourceSettings();

        settings.LoadEnvVar(configuration);

        var resources = settings.Resources.ToDictionary(kv => kv.Key, kv => kv.Value);

        Assert.Equal("service-name-setting", resources[Constants.ResourceAttributes.AttributeServiceName]);
        Assert.Equal("prod", resources["deployment.environment"]);
        Assert.Equal(2, resources.Count);
    }

    [Fact]
    internal void ResourceSettings_LoadEnvVar_DecodesUrlEncodedResourceAttributeValues()
    {
        var configuration = new Configuration(
            false,
            new NameValueConfigurationSource(false, new NameValueCollection
            {
                { ConfigurationKeys.ResourceAttributes, "service.namespace=payments%2Fapi" }
            }));
        var settings = new ResourceSettings();

        settings.LoadEnvVar(configuration);

        var resource = Assert.Single(settings.Resources);
        Assert.Equal("service.namespace", resource.Key);
        Assert.Equal("payments/api", resource.Value);
    }

#if NETFRAMEWORK
    [Fact]
    internal void FromDefaultSources_NonResourceSettingsStillPreferEnvironmentVariablesOverAppSettings()
    {
        using var overrideScope = OverrideAppSettings((ConfigurationKeys.Traces.Exporter, "zipkin"));
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { ConfigurationKeys.Traces.Exporter, "console" }
        });

        var settings = Settings.FromDefaultSources<TracerSettings>(false);

        Assert.Equal([TracesExporter.Console], settings.TracesExporters);
    }

    [Fact]
    internal void FromDefaultSources_ResourceSettingsMergeAppSettingsAfterEnvironmentVariables()
    {
        using var overrideScope = OverrideAppSettings(
            (ConfigurationKeys.ResourceAttributes, "deployment.environment=staging"),
            (ConfigurationKeys.ServiceName, "app-settings-service"));
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { ConfigurationKeys.ResourceAttributes, "deployment.environment=prod,service.namespace=checkout" },
            { ConfigurationKeys.ServiceName, "env-service" }
        });

        var settings = Settings.FromDefaultSources<ResourceSettings>(false);
        var resources = settings.Resources.ToDictionary(kv => kv.Key, kv => kv.Value);

        Assert.Equal("staging", resources["deployment.environment"]);
        Assert.Equal("checkout", resources["service.namespace"]);
        Assert.Equal("app-settings-service", resources[Constants.ResourceAttributes.AttributeServiceName]);
        Assert.Equal(3, resources.Count);
    }

    [Fact]
    internal void FromDefaultSources_ResourceSettingsUseOnlyYamlWhenFileBasedConfigurationIsEnabled()
    {
        var yamlPath = Path.GetTempFileName();
        var yaml = """
resource:
  attributes:
    - name: deployment.environment
      value: yaml
    - name: service.name
      value: yaml-service
""";

        try
        {
            File.WriteAllText(yamlPath, yaml);

            var appDomainSetup = new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.BaseDirectory
            };
            var appDomain = AppDomain.CreateDomain(nameof(FromDefaultSources_ResourceSettingsUseOnlyYamlWhenFileBasedConfigurationIsEnabled), null, appDomainSetup);

            try
            {
                var runner = (SettingsFromDefaultSourcesNetFxRunner)appDomain.CreateInstanceAndUnwrap(
                    typeof(SettingsFromDefaultSourcesNetFxRunner).Assembly.FullName,
                    typeof(SettingsFromDefaultSourcesNetFxRunner).FullName);

                var resources = runner.LoadResourceSettingsFromYaml(yamlPath);

                Assert.Equal("yaml", resources["deployment.environment"]);
                Assert.Equal("yaml-service", resources[Constants.ResourceAttributes.AttributeServiceName]);
                Assert.Equal(2, resources.Count);
            }
            finally
            {
                AppDomain.Unload(appDomain);
            }
        }
        finally
        {
            File.Delete(yamlPath);
        }
    }
#endif

#if NETFRAMEWORK
    [Fact]
    internal void AppSettingsConfigurationSource_ReturnsValueForPresentKey()
    {
        var appSettings = new NameValueCollection
        {
            { ConfigurationKeys.ServiceName, "my-service" }
        };
        var source = new AppSettingsConfigurationSource(false, appSettings);

        Assert.Equal("my-service", source.GetString(ConfigurationKeys.ServiceName));
    }

    [Fact]
    internal void AppSettingsConfigurationSource_ReturnsNullForAbsentKey()
    {
        var source = new AppSettingsConfigurationSource(false, []);

        Assert.Null(source.GetString(ConfigurationKeys.ServiceName));
    }
#endif

    private static void ClearEnvVars()
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.ServiceName, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.ResourceAttributes, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Logs.LogsInstrumentationEnabled, null);
#if NET
        foreach (var logInstrumentation in Enum.GetValues<LogInstrumentation>())
#else
        foreach (var logInstrumentation in Enum.GetValues(typeof(LogInstrumentation)).Cast<LogInstrumentation>())
#endif
        {
            Environment.SetEnvironmentVariable(string.Format(CultureInfo.InvariantCulture, ConfigurationKeys.Logs.EnabledLogsInstrumentationTemplate, logInstrumentation.ToString().ToUpperInvariant()), null);
        }

        Environment.SetEnvironmentVariable(ConfigurationKeys.Logs.Exporter, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Logs.IncludeFormattedMessage, null);

        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.MetricsInstrumentationEnabled, null);
#if NET
        foreach (var metricInstrumentation in Enum.GetValues<MetricInstrumentation>())
#else
        foreach (var metricInstrumentation in Enum.GetValues(typeof(MetricInstrumentation)).Cast<MetricInstrumentation>())
#endif
        {
            Environment.SetEnvironmentVariable(string.Format(CultureInfo.InvariantCulture, ConfigurationKeys.Metrics.EnabledMetricsInstrumentationTemplate, metricInstrumentation.ToString().ToUpperInvariant()), null);
        }

        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.Exporter, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.AdditionalSources, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.TracesInstrumentationEnabled, null);
#if NET
        foreach (var tracerInstrumentation in Enum.GetValues<TracerInstrumentation>())
#else
        foreach (var tracerInstrumentation in Enum.GetValues(typeof(TracerInstrumentation)).Cast<TracerInstrumentation>())
#endif
        {
            Environment.SetEnvironmentVariable(string.Format(CultureInfo.InvariantCulture, ConfigurationKeys.Traces.EnabledTracesInstrumentationTemplate, tracerInstrumentation.ToString().ToUpperInvariant()), null);
        }

        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.Exporter, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.AdditionalLegacySources, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.AdditionalSources, null);
        Environment.SetEnvironmentVariable(AutoOtlpDefinitions.DefaultProtocolEnvVarName, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.FlushOnUnhandledException, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.ResourceDetectorEnabled, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.ProviderPlugins, null);

        Environment.SetEnvironmentVariable(ConfigurationKeys.ResourceDetectorEnabled, null);
#if NET
        foreach (var resourceDetector in Enum.GetValues<ResourceDetector>())
#else
        foreach (var resourceDetector in Enum.GetValues(typeof(ResourceDetector)).Cast<ResourceDetector>())
#endif
        {
            Environment.SetEnvironmentVariable(string.Format(CultureInfo.InvariantCulture, ConfigurationKeys.EnabledResourceDetectorTemplate, resourceDetector.ToString().ToUpperInvariant()), null);
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
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.InstrumentationOptions.SqlClientNetFxILRewriteEnabled, null);

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

#if NETFRAMEWORK
    private static CallbackDisposable OverrideAppSettings(params (string Key, string? Value)[] settings)
    {
        var appSettings = SystemConfigurationManager.AppSettings;
        var originalValues = settings.ToDictionary(setting => setting.Key, setting => appSettings[setting.Key]);
        var originalReadOnly = SetAppSettingsReadOnly(appSettings, false);

        foreach (var (key, value) in settings)
        {
            appSettings[key] = value ?? string.Empty;
        }

        return new CallbackDisposable(() =>
        {
            SetAppSettingsReadOnly(appSettings, false);

            foreach (var originalValue in originalValues)
            {
                var key = originalValue.Key;
                var value = originalValue.Value;
                appSettings[key] = value ?? string.Empty;
            }

            SetAppSettingsReadOnly(appSettings, originalReadOnly);
        });
    }

    private static bool SetAppSettingsReadOnly(NameValueCollection appSettings, bool readOnly)
    {
        var field = typeof(NameObjectCollectionBase).GetField("_readOnly", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? typeof(NameObjectCollectionBase).GetField("readOnly", BindingFlags.Instance | BindingFlags.NonPublic);

        if (field == null)
        {
            throw new InvalidOperationException("Unable to access NameObjectCollectionBase read-only state.");
        }

        var originalReadOnly = (bool)field.GetValue(appSettings)!;
        field.SetValue(appSettings, readOnly);
        return originalReadOnly;
    }

    private sealed class CallbackDisposable(Action onDispose) : IDisposable
    {
        private readonly Action _onDispose = onDispose;

        public void Dispose()
        {
            _onDispose();
        }
    }
#endif
}

#if NETFRAMEWORK
public sealed class SettingsFromDefaultSourcesNetFxRunner : MarshalByRefObject
{
    public Dictionary<string, string> LoadResourceSettingsFromYaml(string yamlPath)
    {
        _ = GetType();
        var originalEnabled = Environment.GetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.Enabled);
        var originalFileName = Environment.GetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.FileName);
        var originalResourceAttributes = Environment.GetEnvironmentVariable(ConfigurationKeys.ResourceAttributes);
        var originalServiceName = Environment.GetEnvironmentVariable(ConfigurationKeys.ServiceName);

        using var appSettingsScope = OverrideAppSettings(
            (ConfigurationKeys.ResourceAttributes, "deployment.environment=appsettings"),
            (ConfigurationKeys.ServiceName, "appsettings-service"));

        try
        {
            Environment.SetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.Enabled, "true");
            Environment.SetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.FileName, yamlPath);
            Environment.SetEnvironmentVariable(ConfigurationKeys.ResourceAttributes, "deployment.environment=env,service.namespace=checkout");
            Environment.SetEnvironmentVariable(ConfigurationKeys.ServiceName, "env-service");

            var settings = Settings.FromDefaultSources<ResourceSettings>(false);
            return settings.Resources.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? string.Empty);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.Enabled, originalEnabled);
            Environment.SetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.FileName, originalFileName);
            Environment.SetEnvironmentVariable(ConfigurationKeys.ResourceAttributes, originalResourceAttributes);
            Environment.SetEnvironmentVariable(ConfigurationKeys.ServiceName, originalServiceName);
        }
    }

    private static CallbackDisposable OverrideAppSettings(params (string Key, string? Value)[] settings)
    {
        var appSettings = SystemConfigurationManager.AppSettings;
        var originalValues = settings.ToDictionary(setting => setting.Key, setting => appSettings[setting.Key]);
        var originalReadOnly = SetAppSettingsReadOnly(appSettings, false);

        foreach (var (key, value) in settings)
        {
            appSettings[key] = value ?? string.Empty;
        }

        return new CallbackDisposable(() =>
        {
            SetAppSettingsReadOnly(appSettings, false);

            foreach (var originalValue in originalValues)
            {
                var key = originalValue.Key;
                var value = originalValue.Value;
                appSettings[key] = value ?? string.Empty;
            }

            SetAppSettingsReadOnly(appSettings, originalReadOnly);
        });
    }

    private static bool SetAppSettingsReadOnly(NameValueCollection appSettings, bool readOnly)
    {
        var field = typeof(NameObjectCollectionBase).GetField("_readOnly", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? typeof(NameObjectCollectionBase).GetField("readOnly", BindingFlags.Instance | BindingFlags.NonPublic);

        if (field == null)
        {
            throw new InvalidOperationException("Unable to access NameObjectCollectionBase read-only state.");
        }

        var originalReadOnly = (bool)field.GetValue(appSettings)!;
        field.SetValue(appSettings, readOnly);
        return originalReadOnly;
    }

    private sealed class CallbackDisposable(Action onDispose) : IDisposable
    {
        private readonly Action _onDispose = onDispose;

        public void Dispose()
        {
            _onDispose();
        }
    }
}
#endif
