using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Execution;
using OpenTelemetry.ClrProfiler.Managed.Configuration;
using OpenTelemetry.Exporter;
using Xunit;

namespace OpenTelemetry.ClrProfiler.Managed.Tests.Configuration
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
        public void FromDefaultSources_DefaultValues()
        {
            var settings = Settings.FromDefaultSources();

            using (new AssertionScope())
            {
                settings.TraceEnabled.Should().BeTrue();
                settings.LoadTracerAtStartup.Should().BeTrue();
                settings.Exporter.Should().Be("otlp");
                settings.OtlpExportProtocol.Should().Be(OtlpExportProtocol.HttpProtobuf);
                settings.OtlpExportEndpoint.Should().Be(new Uri("http://localhost:4318/v1/traces"));
                settings.ConsoleExporterEnabled.Should().BeTrue();
                settings.EnabledInstrumentations.Should().BeEmpty();
                settings.TracerPlugins.Should().BeEmpty();
                settings.ActivitySources.Should().BeEquivalentTo(new List<string> { "OpenTelemetry.ClrProfiler.*" });
                settings.LegacySources.Should().BeEmpty();
                settings.Integrations.Should().NotBeNull();
            }
        }

        [Theory]
        [InlineData("", OtlpExportProtocol.HttpProtobuf, "http://localhost:4318/v1/traces")]
        [InlineData(null, OtlpExportProtocol.HttpProtobuf, "http://localhost:4318/v1/traces")]
        [InlineData("http/protobuf", OtlpExportProtocol.HttpProtobuf, "http://localhost:4318/v1/traces")]
        [InlineData("grpc", null, null)]
        [InlineData("nonExistingProtocol", null, null)]

        public void FromDefaultSources_OtlpExporterPropertiesDependsOnCorrespondingEnvVariables(string otlpProtocol, OtlpExportProtocol? expectedOtlpExportProtocol, string expectedOtlpExportEndpoint)
        {
            Environment.SetEnvironmentVariable(ConfigurationKeys.ExporterOtlpProtocol, otlpProtocol);

            var settings = Settings.FromDefaultSources();

            using (new AssertionScope())
            {
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
        public void FromDefaultSources_OtlpExportEndpointIsNullWhenCorrespondingEnvVarIsSet(string otlpProtocol)
        {
            Environment.SetEnvironmentVariable(ConfigurationKeys.ExporterOtlpProtocol, otlpProtocol);
            Environment.SetEnvironmentVariable(ConfigurationKeys.ExporterOtlpEndpoint, "http://customurl:1234");

            var settings = Settings.FromDefaultSources();

            settings.OtlpExportEndpoint.Should().BeNull();
        }

        private static void ClearEnvVars()
        {
            Environment.SetEnvironmentVariable(ConfigurationKeys.ExporterOtlpProtocol, null);
            Environment.SetEnvironmentVariable(ConfigurationKeys.ExporterOtlpEndpoint, null);
        }
    }
}
