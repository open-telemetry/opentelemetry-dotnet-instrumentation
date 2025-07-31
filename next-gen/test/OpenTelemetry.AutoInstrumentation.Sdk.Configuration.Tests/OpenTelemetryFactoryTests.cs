// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Resources;
using Xunit;

namespace OpenTelemetry.Configuration.Tests;

public sealed class OpenTelemetryFactoryTests
{
    [Fact]
    public void CreateResource_WithBasicAttributes_CreatesResourceWithAttributes()
    {
        // Arrange
        var attributeOptions = new List<OpenTelemetryResourceAttributeOptions>
        {
            new("service.name", "test-service"),
            new("service.version", "1.0.0"),
            new("deployment.environment", "test")
        };
        var resourceOptions = new OpenTelemetryResourceOptions(attributeOptions);

        // Act
        var resource = OpenTelemetryFactory.CreateResource(
            resourceOptions,
            out var unresolvedAttributes);

        // Assert
        Assert.NotNull(resource);
        Assert.Empty(unresolvedAttributes);

        Assert.Equal("test-service", GetAttributeValue(resource, "service.name"));
        Assert.Equal("1.0.0", GetAttributeValue(resource, "service.version"));
        Assert.Equal("test", GetAttributeValue(resource, "deployment.environment"));
    }

    [Fact]
    public void CreateResource_WithEnvironmentVariableReferences_ResolvesFromProvidedEnvironment()
    {
        // Arrange
        var attributeOptions = new List<OpenTelemetryResourceAttributeOptions>
        {
            new("service.name", "$env:SERVICE_NAME"),
            new("service.version", "1.0.0"),
            new("deployment.environment", "$env:ENVIRONMENT")
        };
        var resourceOptions = new OpenTelemetryResourceOptions(attributeOptions);
        var environmentVariables = new Dictionary<string, string>
        {
            ["SERVICE_NAME"] = "my-test-service",
            ["ENVIRONMENT"] = "staging"
        };

        // Act
        var resource = OpenTelemetryFactory.CreateResource(
            resourceOptions,
            out var unresolvedAttributes,
            environmentVariables);

        // Assert
        Assert.NotNull(resource);
        Assert.Empty(unresolvedAttributes);

        Assert.Equal("my-test-service", GetAttributeValue(resource, "service.name"));
        Assert.Equal("1.0.0", GetAttributeValue(resource, "service.version"));
        Assert.Equal("staging", GetAttributeValue(resource, "deployment.environment"));
    }

    [Fact]
    public void CreateResource_WithUnresolvableEnvironmentVariables_ReportsUnresolvedAttributes()
    {
        // Arrange
        var attributeOptions = new List<OpenTelemetryResourceAttributeOptions>
        {
            new("service.name", "test-service"),
            new("service.version", "$env:MISSING_VERSION"),
            new("deployment.environment", "$env:MISSING_ENV")
        };
        var resourceOptions = new OpenTelemetryResourceOptions(attributeOptions);
        var environmentVariables = new Dictionary<string, string>();

        // Act
        var resource = OpenTelemetryFactory.CreateResource(
            resourceOptions,
            out var unresolvedAttributes,
            environmentVariables);

        // Assert
        Assert.NotNull(resource);
        Assert.Equal(2, unresolvedAttributes.Count);
        Assert.Contains(unresolvedAttributes, attr => attr.Key == "service.version");
        Assert.Contains(unresolvedAttributes, attr => attr.Key == "deployment.environment");

        Assert.Equal("test-service", GetAttributeValue(resource, "service.name"));
        Assert.Null(GetAttributeValue(resource, "service.version"));
        Assert.Null(GetAttributeValue(resource, "deployment.environment"));
    }

    [Fact]
    public void CreateResource_WithServiceNameParameter_SetsServiceNameWhenNotInAttributes()
    {
        // Arrange - No service.name in the resource attributes
        var attributeOptions = new List<OpenTelemetryResourceAttributeOptions>
        {
            new("service.version", "1.0.0"),
            new("deployment.environment", "test")
        };
        var resourceOptions = new OpenTelemetryResourceOptions(attributeOptions);

        // Act
        var resource = OpenTelemetryFactory.CreateResource(
            resourceOptions,
            out var unresolvedAttributes,
            serviceName: "fallback-service");

        // Assert
        Assert.NotNull(resource);
        Assert.Empty(unresolvedAttributes);

        Assert.Equal("fallback-service", GetAttributeValue(resource, "service.name"));
        Assert.Equal("1.0.0", GetAttributeValue(resource, "service.version"));
        Assert.Equal("test", GetAttributeValue(resource, "deployment.environment"));
    }

    [Fact]
    public void CreateResource_WithServiceInstanceIdParameter_SetsServiceInstanceId()
    {
        // Arrange
        var attributeOptions = new List<OpenTelemetryResourceAttributeOptions>
        {
            new("service.name", "test-service")
        };
        var resourceOptions = new OpenTelemetryResourceOptions(attributeOptions);

        // Act
        var resource = OpenTelemetryFactory.CreateResource(
            resourceOptions,
            out var unresolvedAttributes,
            serviceInstanceId: "instance-123");

        // Assert
        Assert.NotNull(resource);
        Assert.Empty(unresolvedAttributes);

        Assert.Equal("test-service", GetAttributeValue(resource, "service.name"));
        Assert.Equal("instance-123", GetAttributeValue(resource, "service.instance.id"));
    }

    private static object? GetAttributeValue(Resource resource, string key)
    {
        foreach (var attribute in resource.Attributes)
        {
            if (attribute.Key == key)
            {
                return attribute.Value;
            }
        }

        return null;
    }
}
