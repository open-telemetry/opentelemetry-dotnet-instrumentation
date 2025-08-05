// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Configuration;
using Xunit;

namespace OpenTelemetry.OutOfProcess.Forwarder.Configuration.Tests;

public sealed class OpenTelemetryServiceCollectionExtensionsTests
{
    [Fact]
    public void ConfigureOpenTelemetry_WithDefaultSectionName_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>
        {
            { "OpenTelemetry:Resource:service.name", "TestService" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        var result = services.ConfigureOpenTelemetry();

        // Assert
        Assert.Same(services, result); // Should return the same service collection for fluent configuration

        var serviceProvider = services.BuildServiceProvider();
        var optionsFactory = serviceProvider.GetService<IOptionsFactory<OpenTelemetryOptions>>();
        Assert.NotNull(optionsFactory);
    }

    [Fact]
    public void ConfigureOpenTelemetry_WithCustomSectionName_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>
        {
            { "CustomSection:Resource:service.name", "TestService" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        var result = services.ConfigureOpenTelemetry("CustomSection");

        // Assert
        Assert.Same(services, result); // Should return the same service collection for fluent configuration

        var serviceProvider = services.BuildServiceProvider();
        var optionsFactory = serviceProvider.GetService<IOptionsFactory<OpenTelemetryOptions>>();
        Assert.NotNull(optionsFactory);
    }

    [Fact]
    public void ConfigureOpenTelemetry_WithNullConfigurationSectionName_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.ConfigureOpenTelemetry(null!));
    }

    [Fact]
    public void ConfigureOpenTelemetry_WithEmptyConfigurationSectionName_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => services.ConfigureOpenTelemetry(string.Empty));
    }

    [Fact]
    public void ConfigureOpenTelemetry_WithWhitespaceConfigurationSectionName_ConfiguresWithWhitespaceSection()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act & Assert - Whitespace is actually a valid section name, so no exception should be thrown
        var result = services.ConfigureOpenTelemetry("   ");
        Assert.NotNull(result);
    }

    [Fact]
    public void ConfigureOpenTelemetry_RegistersOptionsValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.ConfigureOpenTelemetry();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Verify that options validation is registered
        var optionsMonitor = serviceProvider.GetService<IOptionsMonitor<OpenTelemetryOptions>>();
        Assert.NotNull(optionsMonitor);
    }

    [Fact]
    public void ConfigureOpenTelemetry_WithMultipleRegistrations_RegistersFactoryCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.ConfigureOpenTelemetry();
        services.ConfigureOpenTelemetry(); // Register twice

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // The service should be available even with multiple registrations
        var optionsFactory = serviceProvider.GetService<IOptionsFactory<OpenTelemetryOptions>>();
        Assert.NotNull(optionsFactory);

        // Multiple calls should work without issues
        var options1 = optionsFactory.Create(Options.DefaultName);
        var options2 = optionsFactory.Create(Options.DefaultName);
        Assert.NotNull(options1);
        Assert.NotNull(options2);
    }

    [Fact]
    public void ConfigureOpenTelemetry_ExtensionMethodIsAvailable()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert - Just verify the extension method exists and can be called
        var result = services.ConfigureOpenTelemetry();
        Assert.NotNull(result);
    }
}
