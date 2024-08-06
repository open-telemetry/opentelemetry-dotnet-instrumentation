// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using FluentAssertions;
using FluentAssertions.Execution;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.Exporter;
using Xunit;

using AutoOtlpDefinitions = OpenTelemetry.AutoInstrumentation.Configurations.Otlp.OtlpSpecConfigDefinitions;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

// use collection to indicate that tests should not be run
// in parallel
// see https://xunit.net/docs/running-tests-in-parallel
[Collection("EventEmittingTests")]
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

        using (new AssertionScope())
        {
            settings.Plugins.Should().BeEmpty();
            settings.EnabledResourceDetectors.Should().NotBeEmpty();
            settings.FlushOnUnhandledException.Should().BeFalse();
        }
    }

    [Fact]
    internal void TracerSettings_DefaultValues()
    {
        var settings = Settings.FromDefaultSources<TracerSettings>(false);

        using (new AssertionScope())
        {
            settings.TracesEnabled.Should().BeTrue();
            settings.TracesExporters.Should().Equal(TracesExporter.Otlp);
            settings.EnabledInstrumentations.Should().NotBeEmpty();
            settings.ActivitySources.Should().BeEquivalentTo(new List<string> { "OpenTelemetry.AutoInstrumentation.*" });
            settings.AdditionalLegacySources.Should().BeEmpty();

            // Instrumentation options tests
#if NETFRAMEWORK
            settings.InstrumentationOptions.AspNetInstrumentationCaptureRequestHeaders.Should().BeEmpty();
            settings.InstrumentationOptions.AspNetInstrumentationCaptureResponseHeaders.Should().BeEmpty();
#endif
#if NET6_0_OR_GREATER
            settings.InstrumentationOptions.AspNetCoreInstrumentationCaptureRequestHeaders.Should().BeEmpty();
            settings.InstrumentationOptions.AspNetCoreInstrumentationCaptureResponseHeaders.Should().BeEmpty();
            settings.InstrumentationOptions.EntityFrameworkCoreSetDbStatementForText.Should().BeFalse();
#endif
            settings.InstrumentationOptions.GraphQLSetDocument.Should().BeFalse();
            settings.InstrumentationOptions.GrpcNetClientInstrumentationCaptureRequestMetadata.Should().BeEmpty();
            settings.InstrumentationOptions.GrpcNetClientInstrumentationCaptureResponseMetadata.Should().BeEmpty();
            settings.InstrumentationOptions.HttpInstrumentationCaptureRequestHeaders.Should().BeEmpty();
            settings.InstrumentationOptions.HttpInstrumentationCaptureResponseHeaders.Should().BeEmpty();
            settings.InstrumentationOptions.OracleMdaSetDbStatementForText.Should().BeFalse();
            settings.InstrumentationOptions.SqlClientSetDbStatementForText.Should().BeFalse();

            settings.OtlpSettings.Should().NotBeNull();
            settings.OtlpSettings!.Protocol.Should().Be(OtlpExportProtocol.HttpProtobuf);
            settings.OtlpSettings.Endpoint.Should().BeNull();
            settings.OtlpSettings.Headers.Should().BeNull();
            settings.OtlpSettings.TimeoutMilliseconds.Should().BeNull();
        }
    }

    [Fact]
    internal void MeterSettings_DefaultValues()
    {
        var settings = Settings.FromDefaultSources<MetricSettings>(false);

        using (new AssertionScope())
        {
            settings.MetricsEnabled.Should().BeTrue();
            settings.MetricExporters.Should().Equal(MetricsExporter.Otlp);
            settings.EnabledInstrumentations.Should().NotBeEmpty();
            settings.Meters.Should().BeEmpty();

            settings.OtlpSettings.Should().NotBeNull();
            settings.OtlpSettings!.Protocol.Should().Be(OtlpExportProtocol.HttpProtobuf);
            settings.OtlpSettings.Endpoint.Should().BeNull();
            settings.OtlpSettings.Headers.Should().BeNull();
            settings.OtlpSettings.TimeoutMilliseconds.Should().BeNull();
        }
    }

    [Fact]
    internal void LogSettings_DefaultValues()
    {
        var settings = Settings.FromDefaultSources<LogSettings>(false);

        using (new AssertionScope())
        {
            settings.LogsEnabled.Should().BeTrue();
            settings.LogExporters.Should().Equal(LogExporter.Otlp);
            settings.EnabledInstrumentations.Should().NotBeEmpty();
            settings.IncludeFormattedMessage.Should().BeFalse();

            settings.OtlpSettings.Should().NotBeNull();
            settings.OtlpSettings!.Protocol.Should().Be(OtlpExportProtocol.HttpProtobuf);
            settings.OtlpSettings.Endpoint.Should().BeNull();
            settings.OtlpSettings.Headers.Should().BeNull();
            settings.OtlpSettings.TimeoutMilliseconds.Should().BeNull();
        }
    }

    [Fact]
    internal void SdkSettings_DefaultValues()
    {
        var settings = Settings.FromDefaultSources<SdkSettings>(false);

        using (new AssertionScope())
        {
            settings.Propagators.Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData("", new[] { TracesExporter.Otlp })]
    [InlineData("none", new TracesExporter[0])]
    [InlineData("non-supported", new TracesExporter[0])]
    [InlineData("otlp", new[] { TracesExporter.Otlp })]
    [InlineData("console", new[] { TracesExporter.Console })]
    [InlineData("zipkin", new[] { TracesExporter.Zipkin })]
    [InlineData("otlp,zipkin,console", new[] { TracesExporter.Otlp, TracesExporter.Zipkin, TracesExporter.Console })]
    [InlineData("none,zipkin", new[] { TracesExporter.Zipkin })]
    [InlineData("otlp,none", new[] { TracesExporter.Otlp })]
    [InlineData("non-supported,none", new TracesExporter[0])]
    [InlineData("zipkin,non-supported,none", new[] { TracesExporter.Zipkin })]
    internal void TracesExporter_SupportedValues(string tracesExporter, TracesExporter[] expectedTracesExporter)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.Exporter, tracesExporter);

        var settings = Settings.FromDefaultSources<TracerSettings>(false);

        settings.TracesExporters.Should().Equal(expectedTracesExporter);
    }

    [Fact]
    internal void TracesExporter_FailFast()
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.Exporter, "not-supported");

        var action = () => Settings.FromDefaultSources<TracerSettings>(true);

        action.Should().Throw<NotSupportedException>();
    }

    [Theory]
    [InlineData("", new[] { MetricsExporter.Otlp })]
    [InlineData("none", new MetricsExporter[0])]
    [InlineData("non-supported", new MetricsExporter[0])]
    [InlineData("otlp", new[] { MetricsExporter.Otlp })]
    [InlineData("prometheus", new[] { MetricsExporter.Prometheus })]
    [InlineData("console", new[] { MetricsExporter.Console })]
    [InlineData("otlp,prometheus,console", new[] { MetricsExporter.Otlp, MetricsExporter.Prometheus, MetricsExporter.Console })]
    [InlineData("prometheus,none", new[] { MetricsExporter.Prometheus })]
    [InlineData("prometheus,non-supported,none", new[] { MetricsExporter.Prometheus })]
    [InlineData("non-supported,none", new MetricsExporter[0])]
    [InlineData("otlp,non-supported,none", new[] { MetricsExporter.Otlp })]
    internal void MetricExporter_SupportedValues(string metricExporter, MetricsExporter[] expectedMetricsExporters)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.Exporter, metricExporter);

        var settings = Settings.FromDefaultSources<MetricSettings>(false);

        settings.MetricExporters.Should().Equal(expectedMetricsExporters);
    }

    [Fact]
    internal void MetricExporter_FailFast()
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.Exporter, "not-supported");

        var action = () => Settings.FromDefaultSources<MetricSettings>(true);

        action.Should().Throw<NotSupportedException>();
    }

    [Theory]
    [InlineData("", new[] { LogExporter.Otlp })]
    [InlineData("none", new LogExporter[0])]
    [InlineData("non-supported", new LogExporter[0])]
    [InlineData("otlp", new[] { LogExporter.Otlp })]
    [InlineData("console", new[] { LogExporter.Console })]
    [InlineData("otlp,none", new[] { LogExporter.Otlp })]
    [InlineData("otlp,console", new[] { LogExporter.Otlp, LogExporter.Console })]
    [InlineData("none,otlp", new[] { LogExporter.Otlp })]
    [InlineData("non-supported,none", new LogExporter[0])]
    [InlineData("non-supported,none,otlp", new[] { LogExporter.Otlp })]
    internal void LogExporter_SupportedValues(string logExporter, LogExporter[] expectedLogExporter)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Logs.Exporter, logExporter);

        var settings = Settings.FromDefaultSources<LogSettings>(false);

        settings.LogExporters.Should().Equal(expectedLogExporter);
    }

    [Fact]
    internal void LogExporter_FailFast()
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Logs.Exporter, "not-supported");

        var action = () => Settings.FromDefaultSources<LogSettings>(true);

        action.Should().Throw<NotSupportedException>();
    }

    [Theory]
    [InlineData(null, new Propagator[] { })]
    [InlineData("not-supported", new Propagator[] { })]
    [InlineData("not-supported1,not-supported2", new Propagator[] { })]
    [InlineData("tracecontext", new[] { Propagator.W3CTraceContext })]
    [InlineData("baggage", new[] { Propagator.W3CBaggage })]
    [InlineData("b3multi", new[] { Propagator.B3Multi })]
    [InlineData("b3", new[] { Propagator.B3Single })]
    [InlineData("not-supported,b3", new Propagator[] { Propagator.B3Single })]
    [InlineData("tracecontext,baggage,b3multi,b3", new[] { Propagator.W3CTraceContext, Propagator.W3CBaggage, Propagator.B3Multi, Propagator.B3Single })]
    internal void Propagators_SupportedValues(string? propagators, Propagator[] expectedPropagators)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Sdk.Propagators, propagators);

        var settings = Settings.FromDefaultSources<SdkSettings>(false);

        settings.Propagators.Should().BeEquivalentTo(expectedPropagators);
    }

    [Fact]
    internal void Propagators_FailFast()
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Sdk.Propagators, "not-supported");

        var action = () => Settings.FromDefaultSources<SdkSettings>(true);

        action.Should().Throw<NotSupportedException>();
    }

    [Theory]
#if NETFRAMEWORK
    [InlineData("ASPNET", TracerInstrumentation.AspNet)]
#endif
#if NET6_0_OR_GREATER
    [InlineData("GRAPHQL", TracerInstrumentation.GraphQL)]
#endif
    [InlineData("HTTPCLIENT", TracerInstrumentation.HttpClient)]
    [InlineData("MONGODB", TracerInstrumentation.MongoDB)]
#if NET6_0_OR_GREATER
    [InlineData("MYSQLDATA", TracerInstrumentation.MySqlData)]
    [InlineData("STACKEXCHANGEREDIS", TracerInstrumentation.StackExchangeRedis)]
#endif
    [InlineData("NPGSQL", TracerInstrumentation.Npgsql)]
    [InlineData("SQLCLIENT", TracerInstrumentation.SqlClient)]
    [InlineData("GRPCNETCLIENT", TracerInstrumentation.GrpcNetClient)]
#if NETFRAMEWORK
    [InlineData("WCFSERVICE", TracerInstrumentation.WcfService)]
#endif
#if NET6_0_OR_GREATER
    [InlineData("MASSTRANSIT", TracerInstrumentation.MassTransit)]
#endif
    [InlineData("NSERVICEBUS", TracerInstrumentation.NServiceBus)]
    [InlineData("ELASTICSEARCH", TracerInstrumentation.Elasticsearch)]
    [InlineData("QUARTZ", TracerInstrumentation.Quartz)]
#if NET6_0_OR_GREATER
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

        settings.EnabledInstrumentations.Should().BeEquivalentTo(new List<TracerInstrumentation> { expectedTracerInstrumentation });
    }

    [Theory]
#if NETFRAMEWORK
    [InlineData("ASPNET", MetricInstrumentation.AspNet)]
#endif
    [InlineData("NETRUNTIME", MetricInstrumentation.NetRuntime)]
    [InlineData("HTTPCLIENT", MetricInstrumentation.HttpClient)]
    [InlineData("PROCESS", MetricInstrumentation.Process)]
    [InlineData("NSERVICEBUS", MetricInstrumentation.NServiceBus)]
#if NET6_0_OR_GREATER
    [InlineData("ASPNETCORE", MetricInstrumentation.AspNetCore)]
#endif
    internal void MeterSettings_Instrumentations_SupportedValues(string meterInstrumentation, MetricInstrumentation expectedMetricInstrumentation)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.MetricsInstrumentationEnabled, "false");
        Environment.SetEnvironmentVariable(string.Format(ConfigurationKeys.Metrics.EnabledMetricsInstrumentationTemplate, meterInstrumentation), "true");

        var settings = Settings.FromDefaultSources<MetricSettings>(false);

        settings.EnabledInstrumentations.Should().BeEquivalentTo(new List<MetricInstrumentation> { expectedMetricInstrumentation });
    }

    [Theory]
    [InlineData("ILOGGER", LogInstrumentation.ILogger)]
    internal void LogSettings_Instrumentations_SupportedValues(string logInstrumentation, LogInstrumentation expectedLogInstrumentation)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Logs.LogsInstrumentationEnabled, "false");
        Environment.SetEnvironmentVariable(string.Format(ConfigurationKeys.Logs.EnabledLogsInstrumentationTemplate, logInstrumentation), "true");

        var settings = Settings.FromDefaultSources<LogSettings>(false);

        settings.EnabledInstrumentations.Should().BeEquivalentTo(new List<LogInstrumentation> { expectedLogInstrumentation });
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    internal void IncludeFormattedMessage_DependsOnCorrespondingEnvVariable(string includeFormattedMessage, bool expectedValue)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Logs.IncludeFormattedMessage, includeFormattedMessage);

        var settings = Settings.FromDefaultSources<LogSettings>(false);

        settings.IncludeFormattedMessage.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData("", OtlpExportProtocol.HttpProtobuf)]
    [InlineData(null, OtlpExportProtocol.HttpProtobuf)]
    [InlineData("http/protobuf", null)]
    [InlineData("grpc", null)]
    [InlineData("nonExistingProtocol", null)]
    internal void OtlpExportProtocol_DependsOnCorrespondingEnvVariable(string? otlpProtocol, OtlpExportProtocol? expectedOtlpExportProtocol)
    {
        Environment.SetEnvironmentVariable(AutoOtlpDefinitions.DefaultProtocolEnvVarName, otlpProtocol);

        var settings = Settings.FromDefaultSources<TracerSettings>(false);

        // null values for expected data will be handled by OTel .NET SDK
        settings.OtlpSettings.Should().NotBeNull();
        settings.OtlpSettings!.Protocol.Should().Be(expectedOtlpExportProtocol);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData(null, false)]
    internal void FlushOnUnhandledException_DependsOnCorrespondingEnvVariable(string? flushOnUnhandledException, bool expectedValue)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.FlushOnUnhandledException, flushOnUnhandledException);

        var settings = Settings.FromDefaultSources<GeneralSettings>(false);

        settings.FlushOnUnhandledException.Should().Be(expectedValue);
    }

    [Theory]
#if NET6_0_OR_GREATER
    [InlineData("CONTAINER", ResourceDetector.Container)]
#endif
    [InlineData("AZUREAPPSERVICE", ResourceDetector.AzureAppService)]
    [InlineData("PROCESSRUNTIME", ResourceDetector.ProcessRuntime)]
    [InlineData("PROCESS", ResourceDetector.Process)]
    [InlineData("OPERATINGSYSTEM", ResourceDetector.OperatingSystem)]
    [InlineData("HOST", ResourceDetector.Host)]
    internal void GeneralSettings_Instrumentations_SupportedValues(string resourceDetector, ResourceDetector expectedResourceDetector)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.ResourceDetectorEnabled, "false");
        Environment.SetEnvironmentVariable(string.Format(ConfigurationKeys.EnabledResourceDetectorTemplate, resourceDetector), "true");

        var settings = Settings.FromDefaultSources<GeneralSettings>(false);

        settings.EnabledResourceDetectors.Should().BeEquivalentTo(new List<ResourceDetector> { expectedResourceDetector });
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

#if NET6_0_OR_GREATER
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.InstrumentationOptions.AspNetCoreInstrumentationCaptureRequestHeaders, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.InstrumentationOptions.AspNetCoreInstrumentationCaptureResponseHeaders, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.InstrumentationOptions.GraphQLSetDocument, null);
#endif
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.InstrumentationOptions.GrpcNetClientInstrumentationCaptureRequestMetadata, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.InstrumentationOptions.GrpcNetClientInstrumentationCaptureResponseMetadata, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.InstrumentationOptions.HttpInstrumentationCaptureRequestHeaders, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.InstrumentationOptions.HttpInstrumentationCaptureResponseHeaders, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.InstrumentationOptions.OracleMdaSetDbStatementForText, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.InstrumentationOptions.SqlClientSetDbStatementForText, null);
    }
}
