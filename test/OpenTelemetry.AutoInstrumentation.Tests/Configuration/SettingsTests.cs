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

using System;
using System.Collections.Generic;
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
        yield return new object[] { ConfigurationKeys.Traces.Exporter, void () => TracerSettings.FromDefaultSources() };
        yield return new object[] { ConfigurationKeys.Traces.Instrumentations, void () => TracerSettings.FromDefaultSources() };
        yield return new object[] { ConfigurationKeys.Metrics.Exporter, void () => MeterSettings.FromDefaultSources() };
        yield return new object[] { ConfigurationKeys.Metrics.Instrumentations, void () => MeterSettings.FromDefaultSources() };
    }

    public void Dispose()
    {
        ClearEnvVars();
    }

    [Fact]
    public void TracerSettings_DefaultValues()
    {
        var settings = TracerSettings.FromDefaultSources();

        using (new AssertionScope())
        {
            settings.TraceEnabled.Should().BeTrue();
            settings.LoadTracerAtStartup.Should().BeTrue();
            settings.TracesExporter.Should().Be(TracesExporter.Otlp);
            settings.OtlpExportProtocol.Should().Be(OtlpExportProtocol.HttpProtobuf);
            settings.ConsoleExporterEnabled.Should().BeFalse();
            settings.EnabledInstrumentations.Should().NotBeEmpty();
            settings.TracerPlugins.Should().BeEmpty();
            settings.ActivitySources.Should().BeEquivalentTo(new List<string> { "OpenTelemetry.AutoInstrumentation.*" });
            settings.LegacySources.Should().BeEmpty();
            settings.Integrations.Should().NotBeNull();
            settings.Http2UnencryptedSupportEnabled.Should().BeFalse();
            settings.FlushOnUnhandledException.Should().BeFalse();
        }
    }

    [Fact]
    public void MeterSettings_DefaultValues()
    {
        var settings = MeterSettings.FromDefaultSources();

        using (new AssertionScope())
        {
            settings.MetricsEnabled.Should().BeTrue();
            settings.LoadMetricsAtStartup.Should().BeTrue();
            settings.MetricExporter.Should().Be(MetricsExporter.Otlp);
            settings.OtlpExportProtocol.Should().Be(OtlpExportProtocol.HttpProtobuf);
            settings.ConsoleExporterEnabled.Should().BeFalse();
            settings.EnabledInstrumentations.Should().BeEmpty();
            settings.MetricPlugins.Should().BeEmpty();
            settings.Meters.Should().BeEmpty();
            settings.Http2UnencryptedSupportEnabled.Should().BeFalse();
            settings.FlushOnUnhandledException.Should().BeFalse();
        }
    }

    [Theory]
    [InlineData("none", TracesExporter.None)]
    [InlineData("jaeger", TracesExporter.Jaeger)]
    [InlineData("otlp", TracesExporter.Otlp)]
    [InlineData("zipkin", TracesExporter.Zipkin)]
    public void TracesExporter_SupportedValues(string tracesExporter, TracesExporter expectedTracesExporter)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.Exporter, tracesExporter);

        var settings = TracerSettings.FromDefaultSources();

        settings.TracesExporter.Should().Be(expectedTracesExporter);
    }

    [Theory]
    [InlineData("none", MetricsExporter.None)]
    [InlineData("otlp", MetricsExporter.Otlp)]
    [InlineData("prometheus", MetricsExporter.Prometheus)]
    public void MetricExporter_SupportedValues(string metricExporter, MetricsExporter expectedMetricsExporter)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.Exporter, metricExporter);

        var settings = MeterSettings.FromDefaultSources();

        settings.MetricExporter.Should().Be(expectedMetricsExporter);
    }

    [Theory]
    [InlineData(nameof(TracerInstrumentation.AspNet), TracerInstrumentation.AspNet)]
    [InlineData(nameof(TracerInstrumentation.GraphQL), TracerInstrumentation.GraphQL)]
    [InlineData(nameof(TracerInstrumentation.HttpClient), TracerInstrumentation.HttpClient)]
    [InlineData(nameof(TracerInstrumentation.MongoDB), TracerInstrumentation.MongoDB)]
    [InlineData(nameof(TracerInstrumentation.Npgsql), TracerInstrumentation.Npgsql)]
    [InlineData(nameof(TracerInstrumentation.SqlClient), TracerInstrumentation.SqlClient)]
    public void TracerSettings_Instrumentations_SupportedValues(string tracerInstrumentation, TracerInstrumentation expectedTracerInstrumentation)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.Instrumentations, tracerInstrumentation);

        var settings = TracerSettings.FromDefaultSources();

        settings.EnabledInstrumentations.Should().BeEquivalentTo(new List<TracerInstrumentation> { expectedTracerInstrumentation });
    }

    [Fact]
    public void TracerSettings_DisabledInstrumentations()
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.Instrumentations, $"{nameof(TracerInstrumentation.AspNet)},{nameof(TracerInstrumentation.GraphQL)}");
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.DisabledInstrumentations, nameof(TracerInstrumentation.GraphQL));

        var settings = TracerSettings.FromDefaultSources();

        settings.EnabledInstrumentations.Should().BeEquivalentTo(new List<TracerInstrumentation> { TracerInstrumentation.AspNet });
    }

    [Theory]
    [InlineData(nameof(MeterInstrumentation.NetRuntime), MeterInstrumentation.NetRuntime)]
    [InlineData(nameof(MeterInstrumentation.AspNet), MeterInstrumentation.AspNet)]
    [InlineData(nameof(MeterInstrumentation.HttpClient), MeterInstrumentation.HttpClient)]
    public void MeterSettings_Instrumentations_SupportedValues(string meterInstrumentation, MeterInstrumentation expectedMeterInstrumentation)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.Instrumentations, meterInstrumentation);

        var settings = MeterSettings.FromDefaultSources();

        settings.EnabledInstrumentations.Should().BeEquivalentTo(new List<MeterInstrumentation> { expectedMeterInstrumentation });
    }

    [Fact]
    public void MeterSettings_DisabledInstrumentations()
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.Instrumentations, $"{nameof(MeterInstrumentation.NetRuntime)},{nameof(MeterInstrumentation.AspNet)}");
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.DisabledInstrumentations, nameof(MeterInstrumentation.AspNet));

        var settings = MeterSettings.FromDefaultSources();

        settings.EnabledInstrumentations.Should().BeEquivalentTo(new List<MeterInstrumentation> { MeterInstrumentation.NetRuntime });
    }

    [Theory]
    [MemberData(nameof(ExporterEnvVarAndLoadSettingsAction))]
    public void UnsupportedExporterValues(string exporterEnvVar, Action loadSettingsAction)
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
    public void OtlpExportProtocol_DependsOnCorrespondingEnvVariable(string otlpProtocol, OtlpExportProtocol? expectedOtlpExportProtocol)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.ExporterOtlpProtocol, otlpProtocol);

        var settings = TracerSettings.FromDefaultSources();

        // null values for expected data will be handled by OTel .NET SDK
        settings.OtlpExportProtocol.Should().Be(expectedOtlpExportProtocol);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData(null, false)]
    public void Http2UnencryptedSupportEnabled_DependsOnCorrespondingEnvVariable(string http2UnencryptedSupportEnabled, bool expectedValue)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Http2UnencryptedSupportEnabled, http2UnencryptedSupportEnabled);

        var settings = TracerSettings.FromDefaultSources();

        settings.Http2UnencryptedSupportEnabled.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData(null, false)]
    public void FlushOnUnhandledException_DependsOnCorrespondingEnvVariable(string flushOnUnhandledException, bool expectedValue)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.FlushOnUnhandledException, flushOnUnhandledException);

        var settings = TracerSettings.FromDefaultSources();

        settings.FlushOnUnhandledException.Should().Be(expectedValue);
    }

    private static void ClearEnvVars()
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.Exporter, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.Instrumentations, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Metrics.DisabledInstrumentations, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.Exporter, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.Instrumentations, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Traces.DisabledInstrumentations, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.ExporterOtlpProtocol, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Http2UnencryptedSupportEnabled, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.FlushOnUnhandledException, null);
    }
}
