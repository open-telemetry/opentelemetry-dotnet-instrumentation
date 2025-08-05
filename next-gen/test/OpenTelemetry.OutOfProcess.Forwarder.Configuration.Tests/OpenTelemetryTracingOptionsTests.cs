// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;

using OpenTelemetry.Configuration;
using Xunit;

namespace OpenTelemetry.OutOfProcess.Forwarder.Configuration.Tests;

public sealed class OpenTelemetryTracingOptionsTests
{
    [Fact]
    public void ParseFromConfig_WithCompleteJsonConfiguration_ParsesAllProperties()
    {
        // Arrange - This JSON shows the complete structure for tracing configuration
        var jsonConfig = """
        {
          "OpenTelemetry": {
            "Tracing": {
              "Sources": [
                "MyApp.Controllers",
                "MyApp.Services",
                "System.Net.Http"
              ],
              "Sampler": {
                "Type": "ParentBased",
                "Settings": {
                  "RootSampler": "TraceIdRatio",
                  "TraceIdRatioBasedSampler": {
                    "SamplingRatio": 0.1
                  }
                }
              },
              "Batch": {
                "MaxQueueSize": 2048,
                "MaxExportBatchSize": 512,
                "ExportIntervalMilliseconds": 1000,
                "ExportTimeoutMilliseconds": 30000
              }
            }
          }
        }
        """;

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonConfig)))
            .Build();
        var tracingSection = configuration.GetSection("OpenTelemetry:Tracing");

        // Act
        var tracingOptions = OpenTelemetryTracingOptions.ParseFromConfig(tracingSection);

        // Assert - Verify sources configuration
        Assert.Equal(3, tracingOptions.Sources.Count);
        Assert.Contains("MyApp.Controllers", tracingOptions.Sources);
        Assert.Contains("MyApp.Services", tracingOptions.Sources);
        Assert.Contains("System.Net.Http", tracingOptions.Sources);

        // Verify sampler configuration
        Assert.NotNull(tracingOptions.SamplerOptions);
        Assert.Equal("ParentBased", tracingOptions.SamplerOptions.SamplerType);

        // Verify batch options
        Assert.NotNull(tracingOptions.BatchOptions);
        Assert.Equal(2048, tracingOptions.BatchOptions.MaxQueueSize);
        Assert.Equal(512, tracingOptions.BatchOptions.MaxExportBatchSize);
        Assert.Equal(1000, tracingOptions.BatchOptions.ExportIntervalMilliseconds);
        Assert.Equal(30000, tracingOptions.BatchOptions.ExportTimeoutMilliseconds);
    }

    [Fact]
    public void ParseFromConfig_WithMinimalJsonConfiguration_UsesDefaults()
    {
        // Arrange - This JSON shows minimal required configuration
        var jsonConfig = """
        {
          "OpenTelemetry": {
            "Tracing": {
              "Sources": [
                "MyApp"
              ]
            }
          }
        }
        """;

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonConfig)))
            .Build();
        var tracingSection = configuration.GetSection("OpenTelemetry:Tracing");

        // Act
        var tracingOptions = OpenTelemetryTracingOptions.ParseFromConfig(tracingSection);

        // Assert - Verify defaults are used
        Assert.Single(tracingOptions.Sources);
        Assert.Contains("MyApp", tracingOptions.Sources);

        // Batch options should use defaults
        Assert.NotNull(tracingOptions.BatchOptions);
        Assert.Equal(2048, tracingOptions.BatchOptions.MaxQueueSize); // Default value
    }

    [Fact]
    public void ParseFromConfig_WithTraceIdRatioSampler_ParsesSamplerCorrectly()
    {
        // Arrange - This JSON shows how to configure trace ID ratio sampling
        var jsonConfig = """
        {
          "OpenTelemetry": {
            "Tracing": {
              "Sources": [
                "MyApp.Critical"
              ],
              "Sampler": {
                "Type": "TraceIdRatio",
                "Settings": {
                  "SamplingRatio": 0.05
                }
              }
            }
          }
        }
        """;

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonConfig)))
            .Build();
        var tracingSection = configuration.GetSection("OpenTelemetry:Tracing");

        // Act
        var tracingOptions = OpenTelemetryTracingOptions.ParseFromConfig(tracingSection);

        // Assert
        Assert.Single(tracingOptions.Sources);
        Assert.Contains("MyApp.Critical", tracingOptions.Sources);
        Assert.Equal("TraceIdRatio", tracingOptions.SamplerOptions.SamplerType);
        Assert.NotNull(tracingOptions.SamplerOptions.TraceIdRatioBasedOptions);
    }

    [Fact]
    public void ParseFromConfig_WithSamplerAsDoubleValue_ParsesCorrectly()
    {
        // Arrange - This JSON shows legacy sampler configuration as a double value
        var jsonConfig = """
        {
          "OpenTelemetry": {
            "Tracing": {
              "Sources": [
                "MyApp.Legacy"
              ],
              "Sampler": "0.25"
            }
          }
        }
        """;

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonConfig)))
            .Build();
        var tracingSection = configuration.GetSection("OpenTelemetry:Tracing");

        // Act
        var tracingOptions = OpenTelemetryTracingOptions.ParseFromConfig(tracingSection);

        // Assert - Should create a parent-based sampler with trace ID ratio
        Assert.Single(tracingOptions.Sources);
        Assert.Contains("MyApp.Legacy", tracingOptions.Sources);
        Assert.Equal("ParentBased", tracingOptions.SamplerOptions.SamplerType);
        Assert.NotNull(tracingOptions.SamplerOptions.ParentBasedOptions);
    }
}
