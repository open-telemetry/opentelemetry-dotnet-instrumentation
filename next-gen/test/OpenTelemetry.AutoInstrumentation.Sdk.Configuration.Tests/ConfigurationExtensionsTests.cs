// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;
using Xunit;

namespace OpenTelemetry.Configuration.Tests;

public sealed class ConfigurationExtensionsTests
{
    [Fact]
    public void TryParseValue_WithValidIntegerValue_ReturnsTrue()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "TestSection:TestKey", "42" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        var section = configuration.GetSection("TestSection");

        // Act
        bool result = section.TryParseValue("TestKey", out int value);

        // Assert
        Assert.True(result);
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryParseValue_WithInvalidIntegerValue_ReturnsFalse()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "TestKey", "not-a-number" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        var section = configuration.GetSection(string.Empty);

        // Act
        bool result = section.TryParseValue("TestKey", out int value);

        // Assert
        Assert.False(result);
        Assert.Equal(0, value);
    }

    [Fact]
    public void TryParseValue_WithMissingKey_ReturnsFalse()
    {
        // Arrange
        var configData = new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        var section = configuration.GetSection(string.Empty);

        // Act
        bool result = section.TryParseValue("MissingKey", out int value);

        // Assert
        Assert.False(result);
        Assert.Equal(0, value);
    }

    [Fact]
    public void TryParseValue_WithEmptyValue_ReturnsFalse()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "TestKey", string.Empty }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        var section = configuration.GetSection(string.Empty);

        // Act
        bool result = section.TryParseValue("TestKey", out int value);

        // Assert
        Assert.False(result);
        Assert.Equal(0, value);
    }

    [Fact]
    public void GetValueOrUseDefault_Bool_WithValidValue_ReturnsConfigValue()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "TestSection:TestKey", "true" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        var section = configuration.GetSection("TestSection");

        // Act
        bool result = section.GetValueOrUseDefault("TestKey", false);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetValueOrUseDefault_Bool_WithInvalidValue_ReturnsDefaultValue()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "TestKey", "not-a-bool" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        var section = configuration.GetSection(string.Empty);

        // Act
        bool result = section.GetValueOrUseDefault("TestKey", true);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetValueOrUseDefault_Bool_WithMissingKey_ReturnsDefaultValue()
    {
        // Arrange
        var configData = new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        var section = configuration.GetSection(string.Empty);

        // Act
        bool result = section.GetValueOrUseDefault("MissingKey", true);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetValueOrUseDefault_Double_WithValidValue_ReturnsConfigValue()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "TestSection:TestKey", "3.14" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        var section = configuration.GetSection("TestSection");

        // Act
        double result = section.GetValueOrUseDefault("TestKey", 2.71);

        // Assert
        Assert.Equal(3.14, result);
    }

    [Fact]
    public void GetValueOrUseDefault_Double_WithInvalidValue_ReturnsDefaultValue()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "TestKey", "not-a-double" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        var section = configuration.GetSection(string.Empty);

        // Act
        double result = section.GetValueOrUseDefault("TestKey", 2.71);

        // Assert
        Assert.Equal(2.71, result);
    }

    [Fact]
    public void GetValueOrUseDefault_Double_WithMissingKey_ReturnsDefaultValue()
    {
        // Arrange
        var configData = new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        var section = configuration.GetSection(string.Empty);

        // Act
        double result = section.GetValueOrUseDefault("MissingKey", 2.71);

        // Assert
        Assert.Equal(2.71, result);
    }
}
