// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;
using OpenTelemetry.Configuration;
using Xunit;

namespace OpenTelemetry.OutOfProcess.Forwarder.Configuration.Tests;

public sealed class OpenTelemetryOptionsFactoryTests
{
    [Fact]
    public void OpenTelemetryOptions_WithBasicResourceConfiguration_ParsesResourceOptions()
    {
        // Test the actual parsing functions that the factory uses
        var jsonConfig = """
        {
          "OpenTelemetry": {
            "Resource": {
              "service.name": "test-service",
              "service.version": "1.0.0"
            }
          }
        }
        """;

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonConfig)))
            .Build();
        var resourceSection = configuration.GetSection("OpenTelemetry:Resource");

        // Act - Test resource parsing directly (same as the factory does)
        var resourceOptions = OpenTelemetryResourceOptions.ParseFromConfig(resourceSection);

        // Assert - Test the core functionality
        Assert.NotNull(resourceOptions);
        Assert.Equal(2, resourceOptions.AttributeOptions.Count);
        Assert.Contains(resourceOptions.AttributeOptions, attr => attr.Key == "service.name" && attr.ValueOrExpression == "test-service");
        Assert.Contains(resourceOptions.AttributeOptions, attr => attr.Key == "service.version" && attr.ValueOrExpression == "1.0.0");
    }

    [Fact]
    public void OpenTelemetryOptions_WithEmptyConfiguration_CreatesEmptyOptions()
    {
        // Test parsing with empty configuration
        var configuration = new ConfigurationBuilder().Build();
        var resourceSection = configuration.GetSection("Resource");

        // Act
        var resourceOptions = OpenTelemetryResourceOptions.ParseFromConfig(resourceSection);

        // Assert
        Assert.NotNull(resourceOptions);
        Assert.Empty(resourceOptions.AttributeOptions);
    }

    [Fact]
    public void OpenTelemetryOptions_WithComplexResourceConfiguration_ParsesAllAttributes()
    {
        // Test complex resource attribute parsing
        var jsonConfig = """
        {
          "OpenTelemetry": {
            "Resource": {
              "service.name": "complex-service",
              "deployment.environment": "production",
              "k8s.cluster.name": "prod-cluster"
            }
          }
        }
        """;

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonConfig)))
            .Build();
        var resourceSection = configuration.GetSection("OpenTelemetry:Resource");

        // Act
        var resourceOptions = OpenTelemetryResourceOptions.ParseFromConfig(resourceSection);

        // Assert
        Assert.NotNull(resourceOptions);
        Assert.Equal(3, resourceOptions.AttributeOptions.Count);
        Assert.Contains(resourceOptions.AttributeOptions, attr => attr.Key == "service.name" && attr.ValueOrExpression == "complex-service");
        Assert.Contains(resourceOptions.AttributeOptions, attr => attr.Key == "deployment.environment" && attr.ValueOrExpression == "production");
        Assert.Contains(resourceOptions.AttributeOptions, attr => attr.Key == "k8s.cluster.name" && attr.ValueOrExpression == "prod-cluster");
    }
}
