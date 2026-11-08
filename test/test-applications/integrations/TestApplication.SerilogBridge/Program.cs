// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Globalization;
#if SERILOG_HOSTING
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
#endif
using Serilog;
#if SERILOG_HOSTING
using Serilog.Extensions.Logging;
#endif
#if SERILOG_SINKS_OPENTELEMETRY
using Serilog.Sinks.OpenTelemetry;
#endif
using TestApplication.Shared;

namespace TestApplication.SerilogBridge;

internal static class Program
{
    private static readonly ActivitySource Source = new("TestApplication.SerilogBridge");
    private static readonly int[] Items = [1, 2, 3];
    private static readonly object[] Points = [new { X = 1 }, new { X = 2 }];

    private static void Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        var mode = ArgumentHelper.GetRequiredArgument(args, "--mode");
        switch (mode)
        {
            case "direct":
                LogUsingSerilogDirectly();
                break;
            case "sub-logger":
                LogThroughSubLogger();
                break;
#if SERILOG_HOSTING
            case "use-serilog":
                LogUsingSerilogHosting();
                break;
            case "static-logger-write-to-providers":
                LogUsingStaticLoggerThatWritesToProviders();
                break;
#endif
#if SERILOG_SINKS_OPENTELEMETRY
            case "opentelemetry-sink":
                LogWithApplicationOpenTelemetrySink();
                break;
#endif
            default:
                throw new NotSupportedException($"{mode} is not supported.");
        }
    }

    private static void LogUsingSerilogDirectly()
    {
        // A culture with a decimal comma, so that a culture-sensitive conversion is visible in the exported values.
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");

        using var rootLogger = new LoggerConfiguration().MinimumLevel.Verbose().CreateLogger();
        var logger = rootLogger.ForContext(typeof(Program));

        using (Source.StartActivity("ManuallyStarted"))
        {
            logger.Information(
                "Hello {Name}, count {Count}, flag {Flag}, ratio {Ratio}, at {When}",
                "world",
                42,
                true,
                1.5,
                new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        }

        logger.Information("Structured {@Point} and {Items}", new { X = 1, Label = "origin" }, Items);
        logger.Information("Nested {@Points} and {@Levels}", Points, new { L2 = new { L3 = new { L4 = new { X = 1 } } } });
        logger.Warning("Logged outside of an activity");
        logger.Error(GetException(), "Exception occurred");
    }

    private static void LogThroughSubLogger()
    {
        using var rootLogger = new LoggerConfiguration()
            .WriteTo.Logger(subLogger => subLogger.MinimumLevel.Verbose())
            .CreateLogger();

        rootLogger.Information("Logged through a root logger with a sub-logger");
    }

#if SERILOG_HOSTING
    private static void LogUsingSerilogHosting()
    {
        using (var host = Host.CreateDefaultBuilder()
                   .UseSerilog((_, configuration) => configuration.MinimumLevel.Verbose())
                   .Build())
        {
            var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(Program));
            logger.LogThroughILogger("hosting");
            Log.Logger.Information("Logged through Serilog {Source}", "hosting");
        }

        Log.CloseAndFlush();
    }

    private static void LogUsingStaticLoggerThatWritesToProviders()
    {
        // The logger is created before the host, so CreateLogger runs before the ILogger bridge is active.
        using var providers = new LoggerProviderCollection();
        Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Providers(providers).CreateLogger();

        // With the ILogger bridge on, the OpenTelemetry provider joins the providers only when the host creates the ILoggerFactory.
        var builder = Host.CreateApplicationBuilder();
        Log.Logger.Information("Logged through Serilog before the host is built");
        builder.Services.AddSerilog(providers: providers);

        using (var host = builder.Build())
        {
            var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(Program));
            logger.LogThroughILogger("hosting");
            Log.Logger.Information("Logged through Serilog {Source}", "hosting");

            using var otherProviders = new LoggerProviderCollection();
            otherProviders.AddProvider(NullLoggerProvider.Instance);
            using var otherLogger = new LoggerConfiguration().WriteTo.Providers(otherProviders).CreateLogger();
            otherLogger.Information("Logged through Serilog to providers that the host does not use");

            // The application adds its own OpenTelemetry provider.
            using var applicationServices = new ServiceCollection().AddLogging(logging => logging.AddOpenTelemetry()).BuildServiceProvider();
            using var applicationProviders = new LoggerProviderCollection();
            applicationProviders.AddProvider(applicationServices.GetRequiredService<ILoggerProvider>());
            using var applicationLogger = new LoggerConfiguration().WriteTo.Providers(applicationProviders).CreateLogger();
            applicationLogger.Information("Logged through Serilog to an application OpenTelemetry provider");
        }

        Log.CloseAndFlush();
    }
#endif

#if SERILOG_SINKS_OPENTELEMETRY
    private static void LogWithApplicationOpenTelemetrySink()
    {
        var endpoint = $"{Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")}/v1/logs";

        using (var rootSinkLogger = new LoggerConfiguration()
                   .WriteTo.OpenTelemetry(options => ConfigureApplicationSink(options, endpoint))
                   .CreateLogger())
        {
            rootSinkLogger.Information("Logged through the application OpenTelemetry sink");
        }

        using (var subLoggerSinkLogger = new LoggerConfiguration()
                   .WriteTo.Logger(subLogger => subLogger.WriteTo.OpenTelemetry(options => ConfigureApplicationSink(options, endpoint)))
                   .CreateLogger())
        {
            subLoggerSinkLogger.Information("Logged through the application OpenTelemetry sink in a sub-logger");
        }

        using (var bridgedLogger = new LoggerConfiguration().CreateLogger())
        {
            bridgedLogger.Information("Logged through a logger without the application OpenTelemetry sink");
        }
    }

    private static void ConfigureApplicationSink(BatchedOpenTelemetrySinkOptions options, string endpoint)
    {
        options.Endpoint = endpoint;
        options.Protocol = OtlpProtocol.HttpProtobuf;
    }
#endif

    private static Exception GetException()
    {
        try
        {
            throw new InvalidOperationException("Example exception for testing");
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
