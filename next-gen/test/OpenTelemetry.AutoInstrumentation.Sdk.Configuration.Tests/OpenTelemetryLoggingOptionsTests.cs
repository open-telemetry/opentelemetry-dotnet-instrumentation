// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;

using Xunit;

namespace OpenTelemetry.Configuration.Tests;

public sealed class OpenTelemetryLoggingOptionsTests
{
    [Fact]
    public void ParseFromConfig_WithCompleteJsonConfiguration_ParsesAllProperties()
    {
        // Arrange - This JSON shows the complete structure for logging configuration
        var jsonConfig = """
        {
          "OpenTelemetry": {
            "Logs": {
              "IncludeScopes": true,
              "Categories": {
                "Microsoft.AspNetCore": "Warning",
                "MyApp.Services": "Information",
                "System": "Error",
                "Default": "Information"
              },
              "Batch": {
                "MaxQueueSize": 1024,
                "MaxExportBatchSize": 256,
                "ExportIntervalMilliseconds": 2000,
                "ExportTimeoutMilliseconds": 15000
              }
            }
          }
        }
        """;

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonConfig)))
            .Build();
        var loggingSection = configuration.GetSection("OpenTelemetry:Logs");

        // Act
        var loggingOptions = OpenTelemetryLoggingOptions.ParseFromConfig(loggingSection);

        // Assert - Verify include scopes
        Assert.True(loggingOptions.IncludeScopes);

        // Verify default log level
        Assert.Equal("Information", loggingOptions.DefaultLogLevel);

        // Verify categories configuration (excludes Default)
        Assert.Equal(3, loggingOptions.CategoryOptions.Count);

        var aspNetCoreCategory = loggingOptions.CategoryOptions.First(c => c.CategoryPrefix == "Microsoft.AspNetCore");
        Assert.Equal("Warning", aspNetCoreCategory.LogLevel);

        var appServicesCategory = loggingOptions.CategoryOptions.First(c => c.CategoryPrefix == "MyApp.Services");
        Assert.Equal("Information", appServicesCategory.LogLevel);

        var systemCategory = loggingOptions.CategoryOptions.First(c => c.CategoryPrefix == "System");
        Assert.Equal("Error", systemCategory.LogLevel);

        // Verify batch options
        Assert.NotNull(loggingOptions.BatchOptions);
        Assert.Equal(1024, loggingOptions.BatchOptions.MaxQueueSize);
        Assert.Equal(256, loggingOptions.BatchOptions.MaxExportBatchSize);
        Assert.Equal(2000, loggingOptions.BatchOptions.ExportIntervalMilliseconds);
        Assert.Equal(15000, loggingOptions.BatchOptions.ExportTimeoutMilliseconds);
    }

    [Fact]
    public void ParseFromConfig_WithMinimalJsonConfiguration_UsesDefaults()
    {
        // Arrange - This JSON shows minimal logging configuration
        var jsonConfig = """
        {
          "OpenTelemetry": {
            "Logs": {
              "Categories": {
                "MyApp": "Information"
              }
            }
          }
        }
        """;

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonConfig)))
            .Build();
        var loggingSection = configuration.GetSection("OpenTelemetry:Logs");

        // Act
        var loggingOptions = OpenTelemetryLoggingOptions.ParseFromConfig(loggingSection);

        // Assert - Verify defaults
        Assert.False(loggingOptions.IncludeScopes); // Default should be false
        Assert.Null(loggingOptions.DefaultLogLevel); // No default specified
        Assert.Single(loggingOptions.CategoryOptions);
        Assert.Equal("MyApp", loggingOptions.CategoryOptions.First().CategoryPrefix);
        Assert.Equal("Information", loggingOptions.CategoryOptions.First().LogLevel);
    }

    [Fact]
    public void ParseFromConfig_WithCategoryFiltering_ConfiguresLogLevels()
    {
        // Arrange - This JSON demonstrates category-based log level filtering
        var jsonConfig = """
        {
          "OpenTelemetry": {
            "Logs": {
              "IncludeScopes": false,
              "Categories": {
                "System": "Error",
                "Microsoft": "Warning",
                "MyApp.Critical": "Debug",
                "Default": "Information"
              }
            }
          }
        }
        """;

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonConfig)))
            .Build();
        var loggingSection = configuration.GetSection("OpenTelemetry:Logs");

        // Act
        var loggingOptions = OpenTelemetryLoggingOptions.ParseFromConfig(loggingSection);

        // Assert - Verify category filtering
        Assert.False(loggingOptions.IncludeScopes);
        Assert.Equal("Information", loggingOptions.DefaultLogLevel);
        Assert.Equal(3, loggingOptions.CategoryOptions.Count); // Excludes Default

        // Verify specific categories and their log levels
        var systemCategory = loggingOptions.CategoryOptions.First(c => c.CategoryPrefix == "System");
        Assert.Equal("Error", systemCategory.LogLevel);

        var microsoftCategory = loggingOptions.CategoryOptions.First(c => c.CategoryPrefix == "Microsoft");
        Assert.Equal("Warning", microsoftCategory.LogLevel);

        var criticalCategory = loggingOptions.CategoryOptions.First(c => c.CategoryPrefix == "MyApp.Critical");
        Assert.Equal("Debug", criticalCategory.LogLevel);
    }
}
