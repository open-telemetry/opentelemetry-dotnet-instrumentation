// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using TestApplication.Shared;

namespace TestApplication.NLogBridge;

internal static class Program
{
    private static readonly ActivitySource Source = new("TestApplication.NLogBridge");

    private static void Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        if (args.Length == 2)
        {
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

        LogInsideActiveScope(() => logger.LogHelloWorld("Hello", "world", DateTime.Now));

        var ex = GetException();
        logger.LogExceptionOccurred(ex);
    }

    private static void LogInsideActiveScope(Action action)
    {
        // Use ActivitySource to create a properly sampled activity
        // The auto-instrumentation sets up an ActivityListener that samples all activities
        using var activity = Source.StartActivity("ManuallyStarted", ActivityKind.Internal);
        action();
    }

    private static void LogUsingNLogDirectly()
    {
        var log = LogManager.GetLogger(typeof(Program).FullName!);

        LogInsideActiveScope(() => log.Info("{0}, {1} at {2:t}!", "Hello", "world", DateTime.Now));

        // Test the Logger.Log(Type wrapperType, LogEventInfo logEvent) overload
        // This exercises the 3-parameter WriteToTargets/WriteLogEventToTargets method
        LogInsideActiveScope(() => LogWithWrapperType(log, "Message via wrapperType overload"));

        var ex = GetException();
        log.Error(ex, "Exception occurred");
    }

    /// <summary>
    /// Logs a message using the Logger.Log(Type wrapperType, LogEventInfo logEvent) overload.
    /// This is used when creating custom logger wrappers.
    /// </summary>
    private static void LogWithWrapperType(Logger logger, string message)
    {
        var logEvent = LogEventInfo.Create(NLog.LogLevel.Info, logger.Name, message);
        logger.Log(typeof(Program), logEvent);
    }

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
