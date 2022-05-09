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
            settings.ConsoleExporterEnabled.Should().BeFalse();
            settings.EnabledInstrumentations.Should().BeEmpty();
            settings.TracerPlugins.Should().BeEmpty();
            settings.ActivitySources.Should().BeEquivalentTo(new List<string> { "OpenTelemetry.AutoInstrumentation.*" });
            settings.LegacySources.Should().BeEmpty();
            settings.Integrations.Should().NotBeNull();
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
        Environment.SetEnvironmentVariable(ConfigurationKeys.Tracer.TracesExporter, tracesExporter);

        var settings = Settings.FromDefaultSources();

        settings.TracesExporter.Should().Be(expectedTracesExporter);
    }

    [Theory]
    [InlineData("not-existing")]
    [InlineData("prometheus")]
    public void TracesExporter_UnsupportedValues(string tracesExporter)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Tracer.TracesExporter, tracesExporter);

        Action act = () => Settings.FromDefaultSources();

        act.Should().Throw<FormatException>();
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

        var settings = Settings.FromDefaultSources();

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

        var settings = Settings.FromDefaultSources();

        settings.Http2UnencryptedSupportEnabled.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData(null, false)]
    public void FlushOnUnhandledException_DependsOnCorrespondingEnvVariable(string flushOnUnhandledException, bool expectedValue)
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.FlushOnUnhandledException, flushOnUnhandledException);

        var settings = Settings.FromDefaultSources();

        settings.FlushOnUnhandledException.Should().Be(expectedValue);
    }

    private static void ClearEnvVars()
    {
        Environment.SetEnvironmentVariable(ConfigurationKeys.Tracer.TracesExporter, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.ExporterOtlpProtocol, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.Http2UnencryptedSupportEnabled, null);
        Environment.SetEnvironmentVariable(ConfigurationKeys.FlushOnUnhandledException, null);
    }
}
