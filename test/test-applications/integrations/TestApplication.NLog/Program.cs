// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;

namespace TestApplication.NLog;

/// <summary>
/// Test application for demonstrating NLog integration with OpenTelemetry auto-instrumentation.
/// This application showcases various logging scenarios to verify that the NLog bridge
/// correctly forwards log events to OpenTelemetry.
/// </summary>
public class Program
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== NLog OpenTelemetry Integration Test Application ===\n");

        // Create and start activity for tracing context
        using var activitySource = new ActivitySource("TestApplication.NLog");
        using var activity = activitySource.StartActivity("NLogDemo");

        try
        {
            // Configure NLog programmatically (in addition to nlog.config)
            ConfigureNLogProgrammatically();

            // Set up .NET Generic Host with NLog
            var host = CreateHostBuilder(args).Build();

            // Demonstrate various logging scenarios
            await DemonstrateNLogLogging(host);

            Console.WriteLine("\n=== Test completed successfully ===");
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Application failed to start or run correctly");
            Console.WriteLine($"Application failed: {ex.Message}");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Creates and configures the host builder with NLog integration.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Configured host builder.</returns>
    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                // Clear default logging providers
                logging.ClearProviders();

                // Add NLog as logging provider
                logging.AddNLog();

                // Set minimum log level
                logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
            })
            .ConfigureServices(services =>
            {
                // Register demo service
                services.AddTransient<IDemoService, DemoService>();
            });

    /// <summary>
    /// Demonstrates various NLog logging scenarios.
    /// </summary>
    /// <param name="host">The configured host.</param>
    private static async Task DemonstrateNLogLogging(IHost host)
    {
        var demoService = host.Services.GetRequiredService<IDemoService>();
        var msLogger = host.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();

        // Test direct NLog usage
        Console.WriteLine("1. Testing direct NLog logging...");
        TestDirectNLogLogging();

        Console.WriteLine("\n2. Testing Microsoft.Extensions.Logging with NLog provider...");
        TestMicrosoftExtensionsLogging(msLogger);

        Console.WriteLine("\n3. Testing structured logging...");
        TestStructuredLogging();

        Console.WriteLine("\n4. Testing logging with exceptions...");
        TestExceptionLogging();

        Console.WriteLine("\n5. Testing logging with custom properties...");
        TestCustomProperties();

        Console.WriteLine("\n6. Testing service-based logging...");
        await demoService.DemonstrateServiceLoggingAsync();

        // Allow time for async log processing
        await Task.Delay(1000);
    }

    /// <summary>
    /// Tests direct NLog logging at various levels.
    /// </summary>
    private static void TestDirectNLogLogging()
    {
        Logger.Trace("This is a TRACE message from NLog");
        Logger.Debug("This is a DEBUG message from NLog");
        Logger.Info("This is an INFO message from NLog");
        Logger.Warn("This is a WARN message from NLog");
        Logger.Error("This is an ERROR message from NLog");
        Logger.Fatal("This is a FATAL message from NLog");
    }

    /// <summary>
    /// Tests Microsoft.Extensions.Logging with NLog provider.
    /// </summary>
    /// <param name="logger">The Microsoft Extensions logger.</param>
    private static void TestMicrosoftExtensionsLogging(Microsoft.Extensions.Logging.ILogger logger)
    {
        logger.LogTrace("This is a TRACE message from Microsoft.Extensions.Logging");
        logger.LogDebug("This is a DEBUG message from Microsoft.Extensions.Logging");
        logger.LogInformation("This is an INFO message from Microsoft.Extensions.Logging");
        logger.LogWarning("This is a WARN message from Microsoft.Extensions.Logging");
        logger.LogError("This is an ERROR message from Microsoft.Extensions.Logging");
        logger.LogCritical("This is a CRITICAL message from Microsoft.Extensions.Logging");
    }

    /// <summary>
    /// Tests structured logging with message templates.
    /// </summary>
    private static void TestStructuredLogging()
    {
        var userId = 12345;
        var userName = "john.doe";
        var actionName = "Login";
        var duration = TimeSpan.FromMilliseconds(250);

        // Structured logging with parameters
        Logger.Info(
            "User {UserId} ({UserName}) performed action {Action} in {Duration}ms",
            userId,
            userName,
            actionName,
            duration.TotalMilliseconds);

        // More complex structured logging
        Logger.Warn(
            "Failed login attempt for user {UserName} from IP {IpAddress} at {Timestamp}",
            userName,
            "192.168.1.100",
            DateTimeOffset.Now);

        // Structured logging with objects
        var contextData = new { RequestId = Guid.NewGuid(), CorrelationId = "abc-123" };
        Logger.Debug("Processing request with context: {@Context}", contextData);
    }

    /// <summary>
    /// Tests logging with exceptions.
    /// </summary>
    private static void TestExceptionLogging()
    {
        try
        {
            // Simulate an exception
            throw new InvalidOperationException("This is a test exception for demonstration purposes");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while processing the demonstration");

            // Nested exception
            try
            {
                throw new ArgumentException("Inner exception", ex);
            }
            catch (Exception innerEx)
            {
                Logger.Fatal(innerEx, "Critical error with nested exception occurred");
            }
        }
    }

    /// <summary>
    /// Tests logging with custom properties using NLog scopes.
    /// </summary>
    private static void TestCustomProperties()
    {
        // Add custom properties using NLog scopes
        using (ScopeContext.PushProperty("CustomProperty1", "Value1"))
        using (ScopeContext.PushProperty("CustomProperty2", 42))
        using (ScopeContext.PushProperty("CustomProperty3", true))
        {
            Logger.Info("Message with custom properties in scope");

            // Nested scope
            using (ScopeContext.PushProperty("NestedProperty", "NestedValue"))
            {
                Logger.Warn("Message with nested custom properties");
            }
        }

        // Properties should be cleared after scope
        Logger.Info("Message after scope (should not have custom properties)");
    }

    /// <summary>
    /// Configures NLog programmatically to demonstrate additional features.
    /// </summary>
    private static void ConfigureNLogProgrammatically()
    {
        // Note: This is in addition to nlog.config file
        var config = LogManager.Configuration;

        // Add global properties
        GlobalDiagnosticsContext.Set("ApplicationName", "TestApplication.NLog");
        GlobalDiagnosticsContext.Set("ApplicationVersion", "1.0.0");
        GlobalDiagnosticsContext.Set("Environment", "Development");

        Logger.Debug("NLog configured programmatically");
    }
}
