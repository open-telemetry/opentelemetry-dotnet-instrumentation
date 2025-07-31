// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;

using Xunit;

namespace OpenTelemetry.Configuration.Tests;

public sealed class OpenTelemetryMetricsOptionsTests
{
    [Fact]
    public void ParseFromConfig_WithCompleteJsonConfiguration_ParsesAllProperties()
    {
        // Arrange - This JSON shows the complete structure for metrics configuration
        var jsonConfig = """
        {
          "OpenTelemetry": {
            "Metrics": {
              "MaxHistograms": 2000,
              "MaxTimeSeries": 5000,
              "Meters": {
                "MyApp.Business": [
                  "requests_total",
                  "response_time"
                ],
                "System.Runtime": [
                  "gc_collections"
                ],
                "Microsoft.AspNetCore.Hosting": []
              },
              "PeriodicExporting": {
                "ExportIntervalMilliseconds": 5000,
                "ExportTimeoutMilliseconds": 2000
              }
            }
          }
        }
        """;

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonConfig)))
            .Build();
        var metricsSection = configuration.GetSection("OpenTelemetry:Metrics");

        // Act
        var metricsOptions = OpenTelemetryMetricsOptions.ParseFromConfig(metricsSection);

        // Assert - Verify metrics limits
        Assert.Equal(2000, metricsOptions.MaxHistograms);
        Assert.Equal(5000, metricsOptions.MaxTimeSeries);

        // Verify meters configuration
        Assert.Equal(3, metricsOptions.MeterOptions.Count);

        var businessMeter = metricsOptions.MeterOptions.First(m => m.MeterName == "MyApp.Business");
        Assert.Equal(2, businessMeter.Instruments.Count);
        Assert.Contains("requests_total", businessMeter.Instruments);
        Assert.Contains("response_time", businessMeter.Instruments);

        var runtimeMeter = metricsOptions.MeterOptions.First(m => m.MeterName == "System.Runtime");
        Assert.Single(runtimeMeter.Instruments);
        Assert.Contains("gc_collections", runtimeMeter.Instruments);

        var aspNetCoreMeter = metricsOptions.MeterOptions.First(m => m.MeterName == "Microsoft.AspNetCore.Hosting");
        Assert.Empty(aspNetCoreMeter.Instruments);

        // Verify periodic exporting options
        Assert.NotNull(metricsOptions.PeriodicExportingOptions);
        Assert.Equal(5000, metricsOptions.PeriodicExportingOptions.ExportIntervalMilliseconds);
        Assert.Equal(2000, metricsOptions.PeriodicExportingOptions.ExportTimeoutMilliseconds);
    }

    [Fact]
    public void ParseFromConfig_WithMinimalJsonConfiguration_UsesDefaults()
    {
        // Arrange - This JSON shows minimal metrics configuration
        var jsonConfig = """
        {
          "OpenTelemetry": {
            "Metrics": {
              "Meters": {
                "MyApp": []
              }
            }
          }
        }
        """;

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonConfig)))
            .Build();
        var metricsSection = configuration.GetSection("OpenTelemetry:Metrics");

        // Act
        var metricsOptions = OpenTelemetryMetricsOptions.ParseFromConfig(metricsSection);

        // Assert - Verify defaults are used
        Assert.Equal(10, metricsOptions.MaxHistograms); // Default value
        Assert.Equal(1000, metricsOptions.MaxTimeSeries); // Default value
        Assert.Single(metricsOptions.MeterOptions);
        Assert.Equal("MyApp", metricsOptions.MeterOptions.First().MeterName);
    }

    [Fact]
    public void ParseFromConfig_WithCustomLimits_AppliesConfiguredLimits()
    {
        // Arrange - This JSON demonstrates custom histogram and time series limits
        var jsonConfig = """
        {
          "OpenTelemetry": {
            "Metrics": {
              "MaxHistograms": 100,
              "MaxTimeSeries": 500,
              "Meters": {
                "MyApp.HighVolume": [
                  "high_frequency_counter"
                ]
              }
            }
          }
        }
        """;

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonConfig)))
            .Build();
        var metricsSection = configuration.GetSection("OpenTelemetry:Metrics");

        // Act
        var metricsOptions = OpenTelemetryMetricsOptions.ParseFromConfig(metricsSection);

        // Assert - Verify custom limits are applied
        Assert.Equal(100, metricsOptions.MaxHistograms);
        Assert.Equal(500, metricsOptions.MaxTimeSeries);
        Assert.Single(metricsOptions.MeterOptions);
        Assert.Equal("MyApp.HighVolume", metricsOptions.MeterOptions.First().MeterName);
        Assert.Contains("high_frequency_counter", metricsOptions.MeterOptions.First().Instruments);
    }
}
