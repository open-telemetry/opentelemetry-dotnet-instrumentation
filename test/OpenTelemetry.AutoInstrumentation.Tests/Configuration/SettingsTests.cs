using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Execution;
using OpenTelemetry.AutoInstrumentation.Configuration;
using OpenTelemetry.Exporter;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configuration
{
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
        public void DefaultValues()
        {
            var settings = Settings.FromDefaultSources();

            using (new AssertionScope())
            {
                settings.TraceEnabled.Should().BeTrue();
                settings.LoadTracerAtStartup.Should().BeTrue();
                settings.TracesExporter.Should().Be(TracesExporter.Otlp);
                settings.OtlpExportProtocol.Should().Be(OtlpExportProtocol.HttpProtobuf);
                settings.OtlpExportEndpoint.Should().Be(new Uri("http://localhost:4318/v1/traces"));
                settings.ConsoleExporterEnabled.Should().BeTrue();
                settings.EnabledInstrumentations.Should().BeEmpty();
                settings.TracerPlugins.Should().BeEmpty();
                settings.ActivitySources.Should().BeEquivalentTo(new List<string> { "OpenTelemetry.AutoInstrumentation.*" });
                settings.LegacySources.Should().BeEmpty();
                settings.Integrations.Should().NotBeNull();
            }
        }

        [Theory]
        [InlineData("none", TracesExporter.None)]
        [InlineData("jaeger", TracesExporter.Jaeger)]
        [InlineData("otlp", TracesExporter.Otlp)]
        [InlineData("zipkin", TracesExporter.Zipkin)]
        public void TracesExporter_SupportedValues(string tracesExporter, TracesExporter expectedTracesExporter)
        {
            Environment.SetEnvironmentVariable(ConfigurationKeys.TracesExporter, tracesExporter);

            var settings = Settings.FromDefaultSources();

            settings.TracesExporter.Should().Be(expectedTracesExporter);
        }

        [Theory]
        [InlineData("not-existing")]
        [InlineData("prometheus")]
        public void TracesExporter_UnsupportedValues(string tracesExporter)
        {
            Environment.SetEnvironmentVariable(ConfigurationKeys.TracesExporter, tracesExporter);

            Action act = () => Settings.FromDefaultSources();

            act.Should().Throw<FormatException>();
        }

        [Theory]
        [InlineData("", OtlpExportProtocol.HttpProtobuf, "http://localhost:4318/v1/traces")]
        [InlineData(null, OtlpExportProtocol.HttpProtobuf, "http://localhost:4318/v1/traces")]
        [InlineData("http/protobuf", OtlpExportProtocol.HttpProtobuf, "http://localhost:4318/v1/traces")]
        [InlineData("grpc", null, null)]
        [InlineData("nonExistingProtocol", null, null)]
        public void OtlpExporterProperties_DependsOnCorrespondingEnvVariables(string otlpProtocol, OtlpExportProtocol? expectedOtlpExportProtocol, string expectedOtlpExportEndpoint)
        {
            Environment.SetEnvironmentVariable(ConfigurationKeys.ExporterOtlpProtocol, otlpProtocol);

            var settings = Settings.FromDefaultSources();

            using (new AssertionScope())
            {
                // null values for expected data will be handled by OTel .NET SDK
                settings.OtlpExportProtocol.Should().Be(expectedOtlpExportProtocol);
                settings.OtlpExportEndpoint.Should().Be(expectedOtlpExportEndpoint != null ? new Uri(expectedOtlpExportEndpoint) : null);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("http/protobuf")]
        [InlineData("grpc")]
        [InlineData("nonExistingProtocol")]
        public void OtlpExportEndpoint_IsNullWhenCorrespondingEnvVarIsSet(string otlpProtocol)
        {
            Environment.SetEnvironmentVariable(ConfigurationKeys.ExporterOtlpProtocol, otlpProtocol);
            Environment.SetEnvironmentVariable(ConfigurationKeys.ExporterOtlpEndpoint, "http://customurl:1234");

            var settings = Settings.FromDefaultSources();

            settings.OtlpExportEndpoint.Should().BeNull("leaving as null as SDK will handle it");
        }

        private static void ClearEnvVars()
        {
            Environment.SetEnvironmentVariable(ConfigurationKeys.TracesExporter, null);
            Environment.SetEnvironmentVariable(ConfigurationKeys.ExporterOtlpProtocol, null);
            Environment.SetEnvironmentVariable(ConfigurationKeys.ExporterOtlpEndpoint, null);
        }
    }
}
