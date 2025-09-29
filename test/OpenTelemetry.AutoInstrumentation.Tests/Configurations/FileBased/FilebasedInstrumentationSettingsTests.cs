// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
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
/*        var instrumentation = new DotNetInstrumentation
        {
            Traces = new DotNetTraces
            {
                GrpcNetClient = new CaptureMetadataConfiguration
                {
                    CaptureRequestMetadata = new[] { "x-request-id", "x-b3-traceid" },
                    CaptureResponseMetadata = new[] { "x-response-id", "x-b3-traceid" }
                },
#if NET
                EntityFrameworkCore = new SetDbStatementForTextConfuguration
                {
                    SetDbStatementForText = true
                },
#endif
                HttpClient = new CaptureHeadersConfiguration
                {
                    CaptureRequestHeaders = new[] { "x-request-id", "x-b3-traceid" },
                    CaptureResponseHeaders = new[] { "x-response-id", "x-b3-traceid" }
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
        Assert.Contains(TracerInstrumentation.Azure, settings.EnabledInstrumentations);
        Assert.Contains(TracerInstrumentation.Elasticsearch, settings.EnabledInstrumentations);*/
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
                Log4Net = new object(),
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
    }
}
