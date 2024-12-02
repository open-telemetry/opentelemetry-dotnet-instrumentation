// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using log4net.Config;
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
            using var activity = Source.StartActivity("ManuallyStarted");

            var logApiName = args[1];
            if (logApiName == "log4net")
            {
                LogUsingLog4NetDirectly();
            }
            else if (logApiName == "ILogger")
            {
                LogUsingILogger();
            }
        }
        else
        {
            throw new ArgumentException("Invalid arguments.");
        }
    }

    private static void LogUsingILogger()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new Log4NetLoggerProvider());
        });
        var logger = loggerFactory.CreateLogger(typeof(Program));

        var (format, arg) = GetInfo();
        logger.LogInformation(format, arg);

        var (message, ex) = GetException();
        logger.LogError(ex, message);
    }

    private static void LogUsingLog4NetDirectly()
    {
        var log = log4net.LogManager.GetLogger(typeof(Program));
        var (format, arg) = GetInfo();
        log.InfoFormat(format, arg);

        var (message, ex) = GetException();
        log.Error(message, ex);
    }

    private static (string Format, string Message) GetInfo()
    {
        return ("Hello, {0}!", "world");
    }

    private static (string Message, Exception Exception) GetException()
    {
        return ("Exception occured", new Exception());
    }
}
