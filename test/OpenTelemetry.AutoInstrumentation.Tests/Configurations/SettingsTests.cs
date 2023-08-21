// <copyright file="SettingsTests.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using FluentAssertions;
using FluentAssertions.Execution;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.Exporter;
using Xunit;

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
            settings.OtlpExportProtocol.Should().Be(OtlpExportProtocol.HttpProtobuf);
        }
    }

    [Fact]
    internal void TracerSettings_DefaultValues()
    {
        var settings = Settings.FromDefaultSources<TracerSettings>(false);

        using (new AssertionScope())
        {
            settings.TracesEnabled.Should().BeTrue();
            settings.TracesExporter.Should().Be(TracesExporter.Otlp);
            settings.OtlpExportProtocol.Should().Be(OtlpExportProtocol.HttpProtobuf);
            settings.ConsoleExporterEnabled.Should().BeFalse();
            settings.EnabledInstrumentations.Should().NotBeEmpty();
            settings.ActivitySources.Should().BeEquivalentTo(new List<string> { "OpenTelemetry.AutoInstrumentation.*" });
            settings.AdditionalLegacySources.Should().BeEmpty();
            settings.TracesSampler.Should().BeNull();
            settings.TracesSamplerArguments.Should().BeNull();

            // Instrumentation options tests
            settings.InstrumentationOptions.GraphQLSetDocument.Should().BeFalse();
        }
    }

    [Fact]
    internal void MeterSettings_DefaultValues()
    {
        var settings = Settings.FromDefaultSources<MetricSettings>(false);

        using (new AssertionScope())
        {
            settings.MetricsEnabled.Should().BeTrue();
            settings.MetricExporter.Should().Be(MetricsExporter.Otlp);
            settings.OtlpExportProtocol.Should().Be(OtlpExportProtocol.HttpProtobuf);
            settings.ConsoleExporterEnabled.Should().BeFalse();
            settings.EnabledInstrumentations.Should().NotBeEmpty();
            settings.Meters.Should().BeEmpty();
        }
    }

    [Fact]
    internal void LogSettings_DefaultValues()
    {
        var settings = Settings.FromDefaultSources<LogSettings>(false);

        using (new AssertionScope())
        {
            settings.LogsEnabled.Should().BeTrue();
            settings.LogExporter.Should().Be(LogExporter.Otlp);
            settings.OtlpExportProtocol.Should().Be(OtlpExportProtocol.HttpProtobuf);
            settings.ConsoleExporterEnabled.Should().BeFalse();
            settings.EnabledInstrumentations.Should().NotBeEmpty();
            settings.IncludeFormattedMessage.Should().BeFalse();
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
    [InlineData("none", TracesExporter.None)]
    [InlineData("non-supported", TracesExporter.Otlp)]
    [InlineData("otlp", TracesExporter.Otlp)]
    [InlineData("zipkin", TracesExporter.Zipkin)]
    internal void TracesExporter_SupportedValues(string tracesExporter, TracesExporter expectedTracesExporter)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.Exporter, tracesExporter);

        var settings = Settings.FromDefaultSources<TracerSettings>(false);

        settings.TracesExporter.Should().Be(expectedTracesExporter);
    }

    [Fact]
    internal void TracesExporter_FailFast()
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.Exporter, "not-supported");

        var action = () => Settings.FromDefaultSources<TracerSettings>(true);

        action.Should().Throw<NotSupportedException>();
    }

    [Theory]
    [InlineData("none", MetricsExporter.None)]
    [InlineData("non-supported", MetricsExporter.Otlp)]
    [InlineData("otlp", MetricsExporter.Otlp)]
    [InlineData("prometheus", MetricsExporter.Prometheus)]
    internal void MetricExporter_SupportedValues(string metricExporter, MetricsExporter expectedMetricsExporter)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.Exporter, metricExporter);

        var settings = Settings.FromDefaultSources<MetricSettings>(false);

        settings.MetricExporter.Should().Be(expectedMetricsExporter);
    }

    [Fact]
    internal void MetricExporter_FailFast()
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.Exporter, "not-supported");

        var action = () => Settings.FromDefaultSources<MetricSettings>(true);

        action.Should().Throw<NotSupportedException>();
    }

    [Theory]
    [InlineData("none", LogExporter.None)]
    [InlineData("non-supported", LogExporter.Otlp)]
    [InlineData("otlp", LogExporter.Otlp)]
    internal void LogExporter_SupportedValues(string logExporter, LogExporter expectedLogExporter)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Logs.Exporter, logExporter);

        var settings = Settings.FromDefaultSources<LogSettings>(false);

        settings.LogExporter.Should().Be(expectedLogExporter);
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
    internal void Propagators_SupportedValues(string propagators, Propagator[] expectedPropagators)
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
#if NETFRAMEWORK
    [InlineData("WCFCLIENT", TracerInstrumentation.WcfClient)]
#endif
    [InlineData("MYSQLCONNECTOR", TracerInstrumentation.MySqlConnector)]
    [InlineData("AZURE", TracerInstrumentation.Azure)]
    internal void TracerSettings_Instrumentations_SupportedValues(string tracerInstrumentation, TracerInstrumentation expectedTracerInstrumentation)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.TracesInstrumentationEnabled, "false");
        Environment.SetEnvironmentVariable(string.Format(ConfigurationKeys.Traces.EnabledTracesInstrumentationTemplate, tracerInstrumentation), "true");

        var settings = Settings.FromDefaultSources<TracerSettings>(false);

        settings.EnabledInstrumentations.Should().BeEquivalentTo(new List<TracerInstrumentation> { expectedTracerInstrumentation });
    }

    [Fact]
    internal void TracerSettings_TracerSampler()
    {
        const string expectedTracesSampler = nameof(expectedTracesSampler);
        const string expectedTracesSamplerArguments = nameof(expectedTracesSamplerArguments);

        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.TracesSampler, expectedTracesSampler);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.TracesSamplerArguments, expectedTracesSamplerArguments);

        var settings = Settings.FromDefaultSources<TracerSettings>(false);

        settings.TracesSampler.Should().Be(expectedTracesSampler);
        settings.TracesSamplerArguments.Should().Be(expectedTracesSamplerArguments);
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
    internal void OtlpExportProtocol_DependsOnCorrespondingEnvVariable(string otlpProtocol, OtlpExportProtocol? expectedOtlpExportProtocol)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.ExporterOtlpProtocol, otlpProtocol);

        var settings = Settings.FromDefaultSources<TracerSettings>(false);

        // null values for expected data will be handled by OTel .NET SDK
        settings.OtlpExportProtocol.Should().Be(expectedOtlpExportProtocol);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData(null, false)]
    internal void FlushOnUnhandledException_DependsOnCorrespondingEnvVariable(string flushOnUnhandledException, bool expectedValue)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.FlushOnUnhandledException, flushOnUnhandledException);

        var settings = Settings.FromDefaultSources<GeneralSettings>(false);

        settings.FlushOnUnhandledException.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData("CONTAINER", ResourceDetector.Container)]
    [InlineData("AZUREAPPSERVICE", ResourceDetector.AzureAppService)]
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
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.TracesSampler, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.TracesSamplerArguments, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.InstrumentationOptions.GraphQLSetDocument, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.ExporterOtlpProtocol, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.FlushOnUnhandledException, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.ResourceDetectorEnabled, null);

        Environment.SetEnvironmentVariable(ConfigurationKeys.ResourceDetectorEnabled, null);
        foreach (var resourceDetector in Enum.GetValues(typeof(ResourceDetector)).Cast<ResourceDetector>())
        {
            Environment.SetEnvironmentVariable(string.Format(ConfigurationKeys.EnabledResourceDetectorTemplate, resourceDetector.ToString().ToUpperInvariant()), null);
        }

        Environment.SetEnvironmentVariable(ConfigurationKeys.Sdk.Propagators, null);
    }
}
