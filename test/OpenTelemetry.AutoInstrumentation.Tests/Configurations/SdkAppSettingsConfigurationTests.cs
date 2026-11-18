// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Collections.Specialized;
using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Tests.Util;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

[Collection("Non-Parallel Collection")]
public sealed class SdkAppSettingsConfigurationTests
{
    private const string SamplerKey = "OTEL_TRACES_SAMPLER";

    [Fact]
    public void AppSettingsValueIsVisibleWhenEnvironmentVariableIsUnset()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?> { { SamplerKey, null } });

        var configuration = SdkAppSettingsConfiguration.Create(new NameValueCollection { { SamplerKey, "always_off" } });

        Assert.Equal("always_off", configuration[SamplerKey]);
        Assert.Null(Environment.GetEnvironmentVariable(SamplerKey));
    }

    [Fact]
    public void EnvironmentVariableTakesPrecedenceOverAppSettings()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?> { { SamplerKey, "always_on" } });

        var configuration = SdkAppSettingsConfiguration.Create(new NameValueCollection { { SamplerKey, "always_off" } });

        Assert.Equal("always_on", configuration[SamplerKey]);
    }

    [Fact]
    public void NonOtelAndProcessWideKeysAreExcluded()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>
        {
            { "SOME_OTHER_SETTING", null },
            { ConfigurationKeys.FailFast, null },
        });

        var configuration = SdkAppSettingsConfiguration.Create(new NameValueCollection
        {
            { "SOME_OTHER_SETTING", "value" },
            { ConfigurationKeys.FailFast, "true" },
        });

        Assert.Null(configuration["SOME_OTHER_SETTING"]);
        Assert.Null(configuration[ConfigurationKeys.FailFast]);
    }

    [Theory]
    [InlineData(null, null, null, false, 73, 23456)]
    [InlineData("always_on", "89", "34567", true, 89, 34567)]
    public void SdkProvidersUseAppSettingsAsFallback(
        string? environmentSampler,
        string? environmentBatchSize,
        string? environmentMetricInterval,
        bool expectedRecorded,
        int expectedBatchSize,
        int expectedMetricInterval)
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>
        {
            { ConfigurationKeys.FileBasedConfiguration.Enabled, null },
            { SamplerKey, environmentSampler },
            { "OTEL_BSP_MAX_QUEUE_SIZE", null },
            { "OTEL_BSP_MAX_EXPORT_BATCH_SIZE", environmentBatchSize },
            { "OTEL_METRIC_EXPORT_INTERVAL", environmentMetricInterval },
        });

        var configPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(
                configPath,
                """
                <configuration>
                  <appSettings>
                    <add key="OTEL_TRACES_SAMPLER" value="always_off" />
                    <add key="OTEL_BSP_MAX_EXPORT_BATCH_SIZE" value="73" />
                    <add key="OTEL_METRIC_EXPORT_INTERVAL" value="23456" />
                  </appSettings>
                </configuration>
                """);

            var setup = new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                ConfigurationFile = configPath
            };
            var domain = AppDomain.CreateDomain(nameof(SdkProvidersUseAppSettingsAsFallback), null, setup);
            try
            {
                var runnerType = typeof(SdkAppSettingsNetFxRunner);
                var runner = (SdkAppSettingsNetFxRunner)domain.CreateInstanceAndUnwrap(runnerType.Assembly.FullName, runnerType.FullName);
                runner.ReadProviderOptions(out var recorded, out var batchSize, out var metricInterval);

                Assert.Equal(expectedRecorded, recorded);
                Assert.Equal(expectedBatchSize, batchSize);
                Assert.Equal(expectedMetricInterval, metricInterval);
            }
            finally
            {
                AppDomain.Unload(domain);
            }

            Assert.Equal(environmentSampler, Environment.GetEnvironmentVariable(SamplerKey));
            Assert.Equal(environmentBatchSize, Environment.GetEnvironmentVariable("OTEL_BSP_MAX_EXPORT_BATCH_SIZE"));
            Assert.Equal(environmentMetricInterval, Environment.GetEnvironmentVariable("OTEL_METRIC_EXPORT_INTERVAL"));
        }
        finally
        {
            File.Delete(configPath);
        }
    }
}

public sealed class SdkAppSettingsNetFxRunner : MarshalByRefObject
{
    public void ReadProviderOptions(out bool recorded, out int batchSize, out int metricInterval)
    {
        var sourceName = GetType().FullName!;
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .UseAppSettingsForSdk()
            .AddSource(sourceName)
            .Build();
        using var activitySource = new ActivitySource(sourceName);
        using var activity = activitySource.StartActivity("test");

        recorded = activity?.Recorded ?? false;
        batchSize = GetTraceBatchSize();
        metricInterval = GetMetricExportInterval();
    }

    private static int GetTraceBatchSize()
    {
        var batchSize = 0;
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .UseAppSettingsForSdk()
            .AddOtlpExporter(options => batchSize = options.BatchExportProcessorOptions.MaxExportBatchSize)
            .Build();
        return batchSize;
    }

    private static int GetMetricExportInterval()
    {
        int? interval = null;
        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .UseAppSettingsForSdk()
            .AddConsoleExporter((_, options) => interval = options.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds)
            .Build();
        return interval.GetValueOrDefault();
    }
}
#endif
