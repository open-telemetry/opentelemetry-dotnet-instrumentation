// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NLog;

namespace TestApplication.NLogBridge;

internal static class Program
{
    private static readonly ActivitySource Source = new("TestApplication.NLogBridge");

    private static void Main(string[] args)
    {
        if (args.Length == 2)
        {
            // Set global context property for testing
            GlobalDiagnosticsContext.Set("test_key", "test_value");

            var logApiName = args[1];
            switch (logApiName)
            {
                case "nlog":
                    LogUsingNLogDirectly();
                    break;
                case "ILogger":
                    LogUsingILogger();
                    break;
                default:
                    throw new NotSupportedException($"{logApiName} is not supported.");
            }
        }
        else
        {
            throw new ArgumentException("Invalid arguments.");
        }
    }

    private static void LogUsingILogger()
    {
        var l = LogManager.GetLogger("TestApplication.NLogBridge");
        l.Warn("Before logger factory is built.");

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new NLogLoggerProvider());
        });
        var logger = loggerFactory.CreateLogger(typeof(Program));

        LogInsideActiveScope(() => logger.LogInformation("{0}, {1} at {2:t}!", "Hello", "world", DateTime.Now));

        var (message, ex) = GetException();
        logger.LogError(ex, message);
    }

    private static void LogInsideActiveScope(Action action)
    {
        using var activity = Source.StartActivity("ManuallyStarted");
        action();
    }

    private static void LogUsingNLogDirectly()
    {
        var log = LogManager.GetLogger(typeof(Program).FullName);

        LogInsideActiveScope(() => log.Info("{0}, {1} at {2:t}!", "Hello", "world", DateTime.Now));

        var (message, ex) = GetException();
        log.Error(ex, message);
    }

    private static (string Message, Exception Exception) GetException()
    {
        try
        {
            throw new InvalidOperationException("Example exception for testing");
        }
        catch (Exception ex)
        {
            return ("Exception occured", ex);
        }
    }
}
