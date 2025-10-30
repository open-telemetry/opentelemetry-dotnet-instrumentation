// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.AutoInstrumentation.HeadersCapture;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased;

public class FilebasedInstrumentationSettingsTests
{
    [Fact]
    public void LoadFile_SetsEnabledTracesInstrumentations_IfPresent()
    {
        var instrumentation = new DotNetInstrumentation
        {
            Traces = new DotNetTraces
            {
                Azure = new object(),
                Elasticsearch = new object()
            }
        };

        var conf = new YamlConfiguration
        {
            InstrumentationDevelopment = new InstrumentationDevelopment
            {
                DotNet = instrumentation
            }
        };

        var settings = new TracerSettings();

        settings.LoadFile(conf);

        Assert.NotNull(settings.EnabledInstrumentations);
        Assert.Contains(TracerInstrumentation.Azure, settings.EnabledInstrumentations);
        Assert.Contains(TracerInstrumentation.Elasticsearch, settings.EnabledInstrumentations);
    }

    [Fact]
    public void LoadFile_SetsEnabledTracesInstrumentationOption()
    {
        var instrumentation = new DotNetInstrumentation
        {
            Traces = new DotNetTraces
            {
                GrpcNetClient = new CaptureMetadataConfiguration
                {
                    CaptureRequestMetadata = "x-request-id",
                    CaptureResponseMetadata = "x-response-id"
                },
                OracleMda = new SetDbStatementForTextConfuguration
                {
                    SetDbStatementForText = true
                },
                HttpClient = new CaptureHeadersConfiguration
                {
                    CaptureRequestHeaders = "x-request-id",
                    CaptureResponseHeaders = "x-response-id"
                }
            }
        };

        var conf = new YamlConfiguration
        {
            InstrumentationDevelopment = new InstrumentationDevelopment
            {
                DotNet = instrumentation
            }
        };

        var settings = new TracerSettings();

        settings.LoadFile(conf);

        Assert.NotNull(settings.EnabledInstrumentations);
        Assert.Contains(TracerInstrumentation.GrpcNetClient, settings.EnabledInstrumentations);
        Assert.Contains(TracerInstrumentation.OracleMda, settings.EnabledInstrumentations);
        Assert.Contains(TracerInstrumentation.HttpClient, settings.EnabledInstrumentations);

        Assert.True(settings.InstrumentationOptions.OracleMdaSetDbStatementForText);
        Assert.Contains(AdditionalTag.CreateGrpcRequestCache("x-request-id"), settings.InstrumentationOptions.GrpcNetClientInstrumentationCaptureRequestMetadata);
        Assert.Contains(AdditionalTag.CreateGrpcResponseCache("x-response-id"), settings.InstrumentationOptions.GrpcNetClientInstrumentationCaptureResponseMetadata);
        Assert.Contains(AdditionalTag.CreateHttpRequestCache("x-request-id"), settings.InstrumentationOptions.HttpInstrumentationCaptureRequestHeaders);
        Assert.Contains(AdditionalTag.CreateHttpResponseCache("x-response-id"), settings.InstrumentationOptions.HttpInstrumentationCaptureResponseHeaders);
    }

    [Fact]
    public void LoadFile_SetsEnabledMetricsInstrumentations_IfPresent()
    {
        var instrumentation = new DotNetInstrumentation
        {
            Metrics = new DotNetMetrics
            {
                HttpClient = new object(),
                NetRuntime = new object()
            }
        };

        var conf = new YamlConfiguration
        {
            InstrumentationDevelopment = new InstrumentationDevelopment
            {
                DotNet = instrumentation
            }
        };

        var settings = new MetricSettings();
        settings.LoadFile(conf);

        Assert.NotNull(settings.EnabledInstrumentations);
        Assert.Contains(MetricInstrumentation.HttpClient, settings.EnabledInstrumentations);
        Assert.Contains(MetricInstrumentation.NetRuntime, settings.EnabledInstrumentations);
    }

    [Fact]
    public void LoadFile_SetsEnabledLogsInstrumentations_IfPresent()
    {
        var instrumentation = new DotNetInstrumentation
        {
            Logs = new DotNetLogs
            {
                ILogger = new object(),
                Log4Net = new()
                {
                    BridgeEnabled = true
                },
            }
        };

        var conf = new YamlConfiguration
        {
            InstrumentationDevelopment = new InstrumentationDevelopment
            {
                DotNet = instrumentation
            }
        };

        var settings = new LogSettings();

        settings.LoadFile(conf);

        Assert.NotNull(settings.EnabledInstrumentations);
        Assert.Contains(LogInstrumentation.ILogger, settings.EnabledInstrumentations);
        Assert.Contains(LogInstrumentation.Log4Net, settings.EnabledInstrumentations);
        Assert.True(settings.EnableLog4NetBridge);
    }
}
