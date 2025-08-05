// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Configuration;
using Xunit;

namespace OpenTelemetry.OutOfProcess.Forwarder.Configuration.Tests;

public sealed class OpenTelemetryResourceAttributeOptionsTests
{
    [Fact]
    public void Constructor_WithValidParameters_SetsProperties()
    {
        // Arrange
        const string key = "service.name";
        const string valueOrExpression = "my-service";

        // Act
        var options = new OpenTelemetryResourceAttributeOptions(key, valueOrExpression);

        // Assert
        Assert.Equal(key, options.Key);
        Assert.Equal(valueOrExpression, options.ValueOrExpression);
    }

    [Fact]
    public void Constructor_WithExpressionValue_SetsValueOrExpression()
    {
        // Arrange
        const string key = "service.version";
        const string expression = "${ASSEMBLY_VERSION}";

        // Act
        var options = new OpenTelemetryResourceAttributeOptions(key, expression);

        // Assert
        Assert.Equal(key, options.Key);
        Assert.Equal(expression, options.ValueOrExpression);
    }

    [Fact]
    public void Constructor_WithEnvironmentVariableExpression_StoresExpression()
    {
        // Arrange
        const string key = "deployment.environment";
        const string envExpression = "${ENVIRONMENT}";

        // Act
        var options = new OpenTelemetryResourceAttributeOptions(key, envExpression);

        // Assert
        Assert.Equal(key, options.Key);
        Assert.Equal(envExpression, options.ValueOrExpression);
    }

    [Theory]
    [InlineData("service.name", "my-application")]
    [InlineData("service.version", "1.0.0")]
    [InlineData("deployment.environment", "production")]
    [InlineData("cloud.provider", "aws")]
    [InlineData("k8s.cluster.name", "production-cluster")]
    public void Constructor_WithVariousResourceAttributes_SetsPropertiesCorrectly(string key, string value)
    {
        // Act
        var options = new OpenTelemetryResourceAttributeOptions(key, value);

        // Assert
        Assert.Equal(key, options.Key);
        Assert.Equal(value, options.ValueOrExpression);
    }
}
