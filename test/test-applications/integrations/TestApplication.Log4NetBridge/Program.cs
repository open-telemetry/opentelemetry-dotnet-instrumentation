// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using log4net;
using Microsoft.Extensions.Logging;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace TestApplication.Log4NetBridge;

internal static class Program
{
    private static readonly ActivitySource Source = new("TestApplication.Log4NetBridge");

    private static void Main(string[] args)
    {
        if (args.Length == 2)
        {
            log4net.GlobalContext.Properties["test_key"] = "test_value";
            var logApiName = args[1];
            switch (logApiName)
            {
                case "log4net":
                    LogUsingLog4NetDirectly();
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
        var l = LogManager.GetLogger("TestApplication.Log4NetBridge");
        l.Warn("Before logger factory is built.");
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new Log4NetLoggerProvider());
        });
        var logger = loggerFactory.CreateLogger(typeof(Program));

        LogInsideActiveScope(() => logger.LogHelloWorld("Hello", "world", DateTime.Now));

        var exception = GetException();
        logger.LogExceptionOccurred(exception);
    }

    private static void LogInsideActiveScope(Action action)
    {
        using var activity = Source.StartActivity("ManuallyStarted");
        action();
    }

    private static void LogUsingLog4NetDirectly()
    {
        var log = log4net.LogManager.GetLogger(typeof(Program));
        LogInsideActiveScope(() => log.InfoFormat("{0}, {1} at {2:t}!", "Hello", "world", DateTime.Now));

        var exception = GetException();
        log.Error("Exception occurred", exception);
    }

    private static Exception GetException()
    {
#pragma warning disable CA2201 // Do not raise reserved exception types
        return new Exception();
#pragma warning restore CA2201 // Do not raise reserved exception types
    }
}
