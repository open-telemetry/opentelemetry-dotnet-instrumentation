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
using OpenTelemetry.AutoInstrumentation.Configuration;
using OpenTelemetry.Exporter;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configuration;

public class SettingsTests : IDisposable
{
    public SettingsTests()
    {
        ClearEnvVars();
    }

    public static IEnumerable<object[]> ExporterEnvVarAndLoadSettingsAction()
    {
        yield return new object[] { ConfigurationKeys.Traces.Exporter, new Action(() => Settings.FromDefaultSources<TracerSettings>()) };
        yield return new object[] { ConfigurationKeys.Traces.Instrumentations, new Action(() => Settings.FromDefaultSources<TracerSettings>()) };
        yield return new object[] { ConfigurationKeys.Metrics.Exporter, new Action(() => Settings.FromDefaultSources<MetricSettings>()) };
        yield return new object[] { ConfigurationKeys.Metrics.Instrumentations, new Action(() => Settings.FromDefaultSources<MetricSettings>()) };
        yield return new object[] { ConfigurationKeys.Logs.Exporter, new Action(() => Settings.FromDefaultSources<LogSettings>()) };
        yield return new object[] { ConfigurationKeys.Sdk.Propagators, new Action(() => Settings.FromDefaultSources<SdkSettings>()) };
    }

    public void Dispose()
    {
        ClearEnvVars();
    }

    [Fact]
    internal void GeneralSettings_DefaultValues()
    {
        var settings = Settings.FromDefaultSources<GeneralSettings>();

        using (new AssertionScope())
        {
            settings.Plugins.Should().BeEmpty();
            settings.FlushOnUnhandledException.Should().BeFalse();
            settings.OtlpExportProtocol.Should().Be(OtlpExportProtocol.HttpProtobuf);
        }
    }

    [Fact]
    internal void TracerSettings_DefaultValues()
    {
        var settings = Settings.FromDefaultSources<TracerSettings>();

        using (new AssertionScope())
        {
            settings.TracesEnabled.Should().BeTrue();
            settings.TracesExporter.Should().Be(TracesExporter.Otlp);
            settings.OtlpExportProtocol.Should().Be(OtlpExportProtocol.HttpProtobuf);
            settings.ConsoleExporterEnabled.Should().BeFalse();
            settings.EnabledInstrumentations.Should().NotBeEmpty();
            settings.ActivitySources.Should().BeEquivalentTo(new List<string> { "OpenTelemetry.AutoInstrumentation.*" });
            settings.LegacySources.Should().BeEmpty();
            settings.TracesSampler.Should().BeNull();
            settings.TracesSamplerArguments.Should().BeNull();

            // Instrumentation options tests
            settings.InstrumentationOptions.GraphQLSetDocument.Should().BeFalse();
        }
    }

    [Fact]
    internal void MeterSettings_DefaultValues()
    {
        var settings = Settings.FromDefaultSources<MetricSettings>();

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
        var settings = Settings.FromDefaultSources<LogSettings>();

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
        var settings = Settings.FromDefaultSources<SdkSettings>();

        using (new AssertionScope())
        {
            settings.Propagators.Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData("none", TracesExporter.None)]
    [InlineData("otlp", TracesExporter.Otlp)]
    [InlineData("zipkin", TracesExporter.Zipkin)]
    internal void TracesExporter_SupportedValues(string tracesExporter, TracesExporter expectedTracesExporter)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.Exporter, tracesExporter);

        var settings = Settings.FromDefaultSources<TracerSettings>();

        settings.TracesExporter.Should().Be(expectedTracesExporter);
    }

    [Theory]
    [InlineData("none", MetricsExporter.None)]
    [InlineData("otlp", MetricsExporter.Otlp)]
    [InlineData("prometheus", MetricsExporter.Prometheus)]
    internal void MetricExporter_SupportedValues(string metricExporter, MetricsExporter expectedMetricsExporter)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.Exporter, metricExporter);

        var settings = Settings.FromDefaultSources<MetricSettings>();

        settings.MetricExporter.Should().Be(expectedMetricsExporter);
    }

    [Theory]
    [InlineData("none", LogExporter.None)]
    [InlineData("otlp", LogExporter.Otlp)]
    internal void LogExporter_SupportedValues(string logExporter, LogExporter expectedLogExporter)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Logs.Exporter, logExporter);

        var settings = Settings.FromDefaultSources<LogSettings>();

        settings.LogExporter.Should().Be(expectedLogExporter);
    }

    [Theory]
    [InlineData(null, new Propagator[] { })]
    [InlineData("tracecontext", new[] { Propagator.W3CTraceContext })]
    [InlineData("baggage", new[] { Propagator.W3CBaggage })]
    [InlineData("b3multi", new[] { Propagator.B3Multi })]
    [InlineData("b3", new[] { Propagator.B3Single })]
    [InlineData("tracecontext,baggage,b3multi,b3", new[] { Propagator.W3CTraceContext, Propagator.W3CBaggage, Propagator.B3Multi, Propagator.B3Single })]
    internal void Propagators_SupportedValues(string propagators, Propagator[] expectedPropagators)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Sdk.Propagators, propagators);

        var settings = Settings.FromDefaultSources<SdkSettings>();

        settings.Propagators.Should().BeEquivalentTo(expectedPropagators);
    }

    [Theory]
    [InlineData(nameof(TracerInstrumentation.AspNet), TracerInstrumentation.AspNet)]
    [InlineData(nameof(TracerInstrumentation.GraphQL), TracerInstrumentation.GraphQL)]
    [InlineData(nameof(TracerInstrumentation.HttpClient), TracerInstrumentation.HttpClient)]
#if NET6_0_OR_GREATER
    [InlineData(nameof(TracerInstrumentation.MongoDB), TracerInstrumentation.MongoDB)]
    [InlineData(nameof(TracerInstrumentation.MySqlData), TracerInstrumentation.MySqlData)]
    [InlineData(nameof(TracerInstrumentation.StackExchangeRedis), TracerInstrumentation.StackExchangeRedis)]
#endif
    [InlineData(nameof(TracerInstrumentation.Npgsql), TracerInstrumentation.Npgsql)]
    [InlineData(nameof(TracerInstrumentation.SqlClient), TracerInstrumentation.SqlClient)]
    [InlineData(nameof(TracerInstrumentation.GrpcNetClient), TracerInstrumentation.GrpcNetClient)]
#if NET6_0_OR_GREATER
    [InlineData(nameof(TracerInstrumentation.MassTransit), TracerInstrumentation.MassTransit)]
#endif
    [InlineData(nameof(TracerInstrumentation.NServiceBus), TracerInstrumentation.NServiceBus)]
    [InlineData(nameof(TracerInstrumentation.Elasticsearch), TracerInstrumentation.Elasticsearch)]
    [InlineData(nameof(TracerInstrumentation.Quartz), TracerInstrumentation.Quartz)]
    internal void TracerSettings_Instrumentations_SupportedValues(string tracerInstrumentation, TracerInstrumentation expectedTracerInstrumentation)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.Instrumentations, tracerInstrumentation);

        var settings = Settings.FromDefaultSources<TracerSettings>();

        settings.EnabledInstrumentations.Should().BeEquivalentTo(new List<TracerInstrumentation> { expectedTracerInstrumentation });
    }

    [Fact]
    internal void TracerSettings_DisabledInstrumentations()
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.Instrumentations, $"{nameof(TracerInstrumentation.AspNet)},{nameof(TracerInstrumentation.GraphQL)}");
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.DisabledInstrumentations, nameof(TracerInstrumentation.GraphQL));

        var settings = Settings.FromDefaultSources<TracerSettings>();

        settings.EnabledInstrumentations.Should().BeEquivalentTo(new List<TracerInstrumentation> { TracerInstrumentation.AspNet });
    }

    [Fact]
    internal void TracerSettings_TracerSampler()
    {
        const string expectedTracesSampler = nameof(expectedTracesSampler);
        const string expectedTracesSamplerArguments = nameof(expectedTracesSamplerArguments);

        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.TracesSampler, expectedTracesSampler);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.TracesSamplerArguments, expectedTracesSamplerArguments);

        var settings = Settings.FromDefaultSources<TracerSettings>();

        settings.TracesSampler.Should().Be(expectedTracesSampler);
        settings.TracesSamplerArguments.Should().Be(expectedTracesSamplerArguments);
    }

    [Theory]
    [InlineData(nameof(MetricInstrumentation.NetRuntime), MetricInstrumentation.NetRuntime)]
    [InlineData(nameof(MetricInstrumentation.AspNet), MetricInstrumentation.AspNet)]
    [InlineData(nameof(MetricInstrumentation.HttpClient), MetricInstrumentation.HttpClient)]
    [InlineData(nameof(MetricInstrumentation.Process), MetricInstrumentation.Process)]
    [InlineData(nameof(MetricInstrumentation.NServiceBus), MetricInstrumentation.NServiceBus)]
    internal void MeterSettings_Instrumentations_SupportedValues(string meterInstrumentation, MetricInstrumentation expectedMetricInstrumentation)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.Instrumentations, meterInstrumentation);

        var settings = Settings.FromDefaultSources<MetricSettings>();

        settings.EnabledInstrumentations.Should().BeEquivalentTo(new List<MetricInstrumentation> { expectedMetricInstrumentation });
    }

    [Fact]
    internal void MeterSettings_DisabledInstrumentations()
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.Instrumentations, $"{nameof(MetricInstrumentation.NetRuntime)},{nameof(MetricInstrumentation.AspNet)}");
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.DisabledInstrumentations, nameof(MetricInstrumentation.AspNet));

        var settings = Settings.FromDefaultSources<MetricSettings>();

        settings.EnabledInstrumentations.Should().BeEquivalentTo(new List<MetricInstrumentation> { MetricInstrumentation.NetRuntime });
    }

    [Theory]
    [InlineData(nameof(LogInstrumentation.ILogger), LogInstrumentation.ILogger)]
    internal void LogSettings_Instrumentations_SupportedValues(string logInstrumentation, LogInstrumentation expectedLogInstrumentation)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.Instrumentations, logInstrumentation);

        var settings = Settings.FromDefaultSources<LogSettings>();

        settings.EnabledInstrumentations.Should().BeEquivalentTo(new List<LogInstrumentation> { expectedLogInstrumentation });
    }

    [Fact]
    internal void LogSettings_DisabledInstrumentations()
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Logs.DisabledInstrumentations, nameof(LogInstrumentation.ILogger));

        var settings = Settings.FromDefaultSources<LogSettings>();

        settings.EnabledInstrumentations.Should().BeEmpty();
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    internal void IncludeFormattedMessage_DependsOnCorrespondingEnvVariable(string includeFormattedMessage, bool expectedValue)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Logs.IncludeFormattedMessage, includeFormattedMessage);

        var settings = Settings.FromDefaultSources<LogSettings>();

        settings.IncludeFormattedMessage.Should().Be(expectedValue);
    }

    [Theory]
    [MemberData(nameof(ExporterEnvVarAndLoadSettingsAction))]
    internal void UnsupportedExporterValues(string exporterEnvVar, Action loadSettingsAction)
    {
        Environment.SetEnvironmentVariable(exporterEnvVar, "not-existing");
        loadSettingsAction.Should().Throw<FormatException>();
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

        var settings = Settings.FromDefaultSources<TracerSettings>();

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

        var settings = Settings.FromDefaultSources<GeneralSettings>();

        settings.FlushOnUnhandledException.Should().Be(expectedValue);
    }

    private static void ClearEnvVars()
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Logs.Exporter, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Logs.IncludeFormattedMessage, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Logs.Instrumentations, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Logs.DisabledInstrumentations, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.Exporter, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.Instrumentations, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.DisabledInstrumentations, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.Exporter, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.Instrumentations, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.DisabledInstrumentations, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.TracesSampler, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.TracesSamplerArguments, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.InstrumentationOptions.GraphQLSetDocument, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.ExporterOtlpProtocol, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.FlushOnUnhandledException, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Sdk.Propagators, null);
    }
}
