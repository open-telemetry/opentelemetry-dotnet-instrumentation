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
        // Create an Activity manually to ensure trace context is available
        // This simulates what happens in a real distributed tracing scenario
        var activity = new Activity("ManuallyStarted");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        // Verify the activity is current
        Console.WriteLine($"Activity.Current is null: {Activity.Current == null}");
        if (Activity.Current != null)
        {
            Console.WriteLine($"TraceId: {Activity.Current.TraceId}, SpanId: {Activity.Current.SpanId}");
        }

        try
        {
            action();
        }
        finally
        {
            activity.Stop();
        }
    }

    private static void LogUsingNLogDirectly()
    {
        var log = LogManager.GetLogger(typeof(Program).FullName ?? "TestApplication.NLogBridge.Program");

        Console.WriteLine("=== COMPREHENSIVE NLOG INTEGRATION TEST ===");
        Console.WriteLine($"NLog Version: {typeof(LogManager).Assembly.GetName().Version}");
        var firstRule = log.Factory.Configuration?.LoggingRules?.FirstOrDefault();
        var firstLevel = firstRule?.Levels?.FirstOrDefault();
        Console.WriteLine($"Current Log Level: {firstLevel?.ToString() ?? "Unknown"}");
        Console.WriteLine();

        // Test 1: All NLog convenience methods with ALL log levels
        Console.WriteLine("TEST 1: Testing ALL NLog convenience methods (including Trace/Debug)...");
        LogInsideActiveScope(() =>
        {
            log.Trace("🔍 Trace message from convenience method - detailed execution flow");
            log.Debug("🐛 Debug message from convenience method - debugging information");
            log.Info("ℹ️ Info message with parameter: {Parameter}", "test_param_value");
            log.Warn("⚠️ Warning message from convenience method - potential issue");
            log.Error("❌ Error message from convenience method - error occurred");
            log.Fatal("💀 Fatal message from convenience method - critical failure");
        });

        // Test 2: Exception handling with different log levels
        Console.WriteLine("TEST 2: Testing exception handling across log levels...");
        var (message, ex) = GetException();
        log.Trace(ex, "Trace level exception: {Message}", message);
        log.Debug(ex, "Debug level exception: {Message}", message);
        log.Error(ex, "Error level exception: {Message}", message);
        log.Fatal(ex, "Fatal level exception: {Message}", message);

        // Test 3: Structured logging with complex objects
        Console.WriteLine("TEST 3: Testing structured logging with complex data...");
        var user = new { UserId = 12345, UserName = "john.doe", Email = "john@example.com" };
        var loginData = new { Timestamp = DateTime.Now, IpAddress = "192.168.1.100", UserAgent = "Mozilla/5.0" };

        log.Trace("User trace: {@User} from {@LoginData}", user, loginData);
        log.Debug("User debug: {@User} from {@LoginData}", user, loginData);
        log.Info(
            "User {UserId} ({UserName}) logged in at {LoginTime} from {IpAddress}",
            user.UserId,
            user.UserName,
            loginData.Timestamp,
            loginData.IpAddress);
        log.Warn("Suspicious login attempt for user {UserId} from {IpAddress}", user.UserId, loginData.IpAddress);

        // Test 4: Explicit Logger.Log(LogEventInfo) calls for all levels
        Console.WriteLine("TEST 4: Testing explicit Logger.Log(LogEventInfo) for all levels...");
        LogInsideActiveScope(() =>
        {
            // Trace level
            var traceEvent = new LogEventInfo(NLog.LogLevel.Trace, log.Name, "Explicit trace LogEventInfo: {Operation}");
            traceEvent.Parameters = new object[] { "database_query" };
            traceEvent.Properties["operation_id"] = Guid.NewGuid();
            log.Log(traceEvent);

            // Debug level
            var debugEvent = new LogEventInfo(NLog.LogLevel.Debug, log.Name, "Explicit debug LogEventInfo: {Component}");
            debugEvent.Parameters = new object[] { "authentication_service" };
            debugEvent.Properties["debug_context"] = "user_validation";
            log.Log(debugEvent);

            // Info level
            var infoEvent = new LogEventInfo(NLog.LogLevel.Info, log.Name, "Explicit info LogEventInfo: Hello, {Name} at {Time:t}!");
            infoEvent.Parameters = new object[] { "world", DateTime.Now };
            infoEvent.Properties["request_id"] = "req_123456";
            log.Log(infoEvent);

            // Warn level
            var warnEvent = new LogEventInfo(NLog.LogLevel.Warn, log.Name, "Explicit warn LogEventInfo: {WarningType}");
            warnEvent.Parameters = new object[] { "rate_limit_approaching" };
            warnEvent.Properties["threshold"] = 0.8;
            log.Log(warnEvent);

            // Error level
            var errorEvent = new LogEventInfo(NLog.LogLevel.Error, log.Name, "Explicit error LogEventInfo: {ErrorType}");
            errorEvent.Parameters = new object[] { "validation_failed" };
            errorEvent.Exception = ex;
            errorEvent.Properties["error_code"] = "VAL_001";
            log.Log(errorEvent);

            // Fatal level
            var fatalEvent = new LogEventInfo(NLog.LogLevel.Fatal, log.Name, "Explicit fatal LogEventInfo: {FatalError}");
            fatalEvent.Parameters = new object[] { "system_shutdown" };
            fatalEvent.Exception = ex;
            fatalEvent.Properties["shutdown_reason"] = "critical_error";
            log.Log(fatalEvent);
        });

        // Test 5: Performance test with rapid logging
        Console.WriteLine("TEST 5: Performance test - rapid logging across all levels...");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < 10; i++)
        {
            log.Trace("Performance trace {Index}: operation_{Operation}", i, $"op_{i}");
            log.Debug("Performance debug {Index}: component_{Component}", i, $"comp_{i}");
            log.Info("Performance info {Index}: request_{RequestId}", i, $"req_{i}");
            log.Warn("Performance warn {Index}: threshold_{Threshold}", i, i * 0.1);
            log.Error("Performance error {Index}: error_{ErrorCode}", i, $"ERR_{i:D3}");

            // Mix in some explicit Logger.Log calls
            if (i % 3 == 0)
            {
                var perfEvent = new LogEventInfo(NLog.LogLevel.Info, log.Name, "Performance explicit log {Index}");
                perfEvent.Parameters = new object[] { i };
                perfEvent.Properties["batch_id"] = $"batch_{i / 3}";
                perfEvent.Properties["performance_test"] = true;
                log.Log(perfEvent);
            }
        }

        stopwatch.Stop();
        log.Info(
            "Performance test completed in {ElapsedMs}ms - {LogCount} log entries",
            stopwatch.ElapsedMilliseconds,
            60); // 50 convenience + 10 explicit

        // Test 6: Edge cases and special scenarios
        Console.WriteLine("TEST 6: Testing edge cases and special scenarios...");

        // Null and empty parameters
        log.Info("Testing null parameter: {NullValue}", (string?)null);
        log.Debug("Testing empty string: '{EmptyValue}'", string.Empty);

        // Large objects
        var largeObject = new
        {
            Data = string.Join(string.Empty, Enumerable.Range(0, 100).Select(i => $"item_{i}_")),
            Metadata = Enumerable.Range(0, 50).ToDictionary(i => $"key_{i}", i => $"value_{i}")
        };
        log.Trace("Large object test: {@LargeObject}", largeObject);

        // Unicode and special characters
        log.Info("Unicode test: {Message}", "Hello 世界! 🌍 Ñoël 中文 العربية русский");

        // Multiple exceptions
        try
        {
            try
            {
                throw new ArgumentException("Inner exception");
            }
            catch (Exception inner)
            {
                throw new InvalidOperationException("Outer exception", inner);
            }
        }
        catch (Exception nestedEx)
        {
            log.Error(nestedEx, "Nested exception test: {Context}", "multiple_exceptions");
        }

        Console.WriteLine("TEST 7: Final batch to trigger export...");
        // Generate final batch to ensure everything is exported
        for (int i = 0; i < 5; i++)
        {
            log.Info("Final batch message {Index} - ensuring export", i + 1);
        }

        // Add longer delay to ensure all logs are processed and exported
        Console.WriteLine("Waiting for batch processor to flush all logs...");
        System.Threading.Thread.Sleep(5000);

        Console.WriteLine("=== COMPREHENSIVE TEST COMPLETED ===");
        Console.WriteLine("Check Grafana Cloud for all log entries!");
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
