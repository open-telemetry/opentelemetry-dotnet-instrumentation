// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Xunit;

namespace OpenTelemetry.Configuration.Tests;

public sealed class OpenTelemetryOptionsTests
{
    [Fact]
    public void OpenTelemetryOptions_PublicProperties_AreReadOnly()
    {
        // Act & Assert - Verify all public properties are read-only
        var type = typeof(OpenTelemetryOptions);
        var properties = type.GetProperties();

        foreach (var property in properties)
        {
            Assert.False(property.CanWrite, $"Property {property.Name} should be read-only");
            Assert.True(property.CanRead, $"Property {property.Name} should be readable");
        }
    }

    [Fact]
    public void OpenTelemetryOptions_HasExpectedPublicProperties()
    {
        // Arrange
        var type = typeof(OpenTelemetryOptions);

        // Act & Assert - Verify expected public properties exist
        Assert.NotNull(type.GetProperty(nameof(OpenTelemetryOptions.ResourceOptions)));
        Assert.NotNull(type.GetProperty(nameof(OpenTelemetryOptions.LoggingOptions)));
        Assert.NotNull(type.GetProperty(nameof(OpenTelemetryOptions.MetricsOptions)));
        Assert.NotNull(type.GetProperty(nameof(OpenTelemetryOptions.TracingOptions)));
        Assert.NotNull(type.GetProperty(nameof(OpenTelemetryOptions.ExporterOptions)));
    }

    [Fact]
    public void OpenTelemetryOptions_PropertiesHaveCorrectTypes()
    {
        // Arrange
        var type = typeof(OpenTelemetryOptions);

        // Act & Assert
        Assert.Equal(typeof(OpenTelemetryResourceOptions), type.GetProperty(nameof(OpenTelemetryOptions.ResourceOptions))?.PropertyType);
        Assert.Equal(typeof(OpenTelemetryLoggingOptions), type.GetProperty(nameof(OpenTelemetryOptions.LoggingOptions))?.PropertyType);
        Assert.Equal(typeof(OpenTelemetryMetricsOptions), type.GetProperty(nameof(OpenTelemetryOptions.MetricsOptions))?.PropertyType);
        Assert.Equal(typeof(OpenTelemetryTracingOptions), type.GetProperty(nameof(OpenTelemetryOptions.TracingOptions))?.PropertyType);
        Assert.Equal(typeof(OpenTelemetryExporterOptions), type.GetProperty(nameof(OpenTelemetryOptions.ExporterOptions))?.PropertyType);
    }
}
