// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logging;
using OpenTelemetry.OpenTelemetryProtocol.Logging;
using Xunit;

using LogRecord = OpenTelemetry.Logging.LogRecord;
using OtlpLogs = OpenTelemetry.Proto.Logs.V1;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests;

public sealed partial class ProtobufOtlpLogRecordExporterAsyncTests : IDisposable
{
    private static LogRecordSeverity ToOtelSeverity(LogLevel level) => level switch
    {
        LogLevel.Trace => LogRecordSeverity.Trace,
        LogLevel.Debug => LogRecordSeverity.Debug,
        LogLevel.Information => LogRecordSeverity.Info,
        LogLevel.Warning => LogRecordSeverity.Warn,
        LogLevel.Error => LogRecordSeverity.Error,
        LogLevel.Critical => LogRecordSeverity.Fatal,
        LogLevel.None => LogRecordSeverity.Info,
        _ => LogRecordSeverity.Unspecified,
    };

    private readonly ILogger _Logger;
    private readonly TestLogRecordProcessor _LogRecordProcessor;
    private readonly ILoggerFactory _LoggerFactory;

    public ProtobufOtlpLogRecordExporterAsyncTests()
    {
        // Create a log record processor that will capture log records for testing
        _LogRecordProcessor = new TestLogRecordProcessor();

        // Set up logger factory with our test processor
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddProvider(new TestLoggerProvider(_LogRecordProcessor));
        });

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        _LoggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        // Create logger for tests
        _Logger = _LoggerFactory.CreateLogger("TestLogger");
    }

    public void Dispose()
    {
        _LogRecordProcessor.Dispose();
        (_LoggerFactory as IDisposable)?.Dispose();
    }

    [Fact]
    public void SimpleLogToOtlpLogRecord()
    {
        // Log a simple message through ILogger
        LogMessages.LogTestMessage(_Logger);

        // Ensure the log was captured
        Assert.Single(_LogRecordProcessor.BufferedLogRecords);
        BufferedLogRecord bufferedLogRecord = _LogRecordProcessor.BufferedLogRecords[0];
        bufferedLogRecord.ToLogRecord(out LogRecord logRecord);

        // Convert to OTLP format
        OtlpLogs.LogRecord? otlpLogRecord = ToOtlpLogRecord(logRecord);

        // Verify the conversion
        Assert.NotNull(otlpLogRecord);
        Assert.Equal("Test log message", otlpLogRecord.Body.StringValue);
        Assert.Equal("Information", otlpLogRecord.SeverityText);
        Assert.Equal(OtlpLogs.SeverityNumber.Info, otlpLogRecord.SeverityNumber);
    }

    // Update the StructuredLogToOtlpLogRecord method to use the LoggerMessage delegate
    [Fact]
    public void StructuredLogToOtlpLogRecord()
    {
        LogMessages.LogProductPrice(_Logger, "apple", 1.99);

        // Ensure the log was captured
        Assert.Single(_LogRecordProcessor.BufferedLogRecords);
        BufferedLogRecord bufferedLogRecord = _LogRecordProcessor.BufferedLogRecords[0];
        bufferedLogRecord.ToLogRecord(out LogRecord logRecord);

        // Convert to OTLP format
        OtlpLogs.LogRecord? otlpLogRecord = ToOtlpLogRecord(logRecord);

        // Verify the message and attributes
        Assert.NotNull(otlpLogRecord);
        Assert.Equal("Product apple price is now 1.99", otlpLogRecord.Body.StringValue);

        // Verify attributes from structured logging
        Proto.Common.V1.KeyValue? productNameAttr = otlpLogRecord.Attributes.FirstOrDefault(a => a.Key == "ProductName");
        Assert.NotNull(productNameAttr);
        Assert.Equal("apple", productNameAttr.Value.StringValue);

        Proto.Common.V1.KeyValue? priceAttr = otlpLogRecord.Attributes.FirstOrDefault(a => a.Key == "Price");
        Assert.NotNull(priceAttr);
        Assert.Equal(1.99, priceAttr.Value.DoubleValue);
    }

    // Add a static class to define LoggerMessage delegates for improved performance
    internal static partial class LogMessages
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Product {ProductName} price is now {Price}")]
        internal static partial void LogProductPrice(ILogger logger, string productName, double price);

        [LoggerMessage(Level = LogLevel.Information, Message = "Test log message")]
        internal static partial void LogTestMessage(ILogger logger);
    }

    /// <summary>
    /// Test log record processor that captures log records for testing.
    /// </summary>
    private sealed class TestLogRecordProcessor : IProcessor
    {
        public List<BufferedLogRecord> BufferedLogRecords = [];

        public static ValueTask<bool> OnEndAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(true);
        }

        public static ValueTask<bool> OnForceFlushAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(true);
        }

        public bool ProcessLogRecord(in LogRecord logRecord)
        {
            BufferedLogRecords.Add(new BufferedLogRecord(logRecord));
            return true;
        }

        public static void Shutdown()
        {
        }

        public Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task ShutdownAsync(CancellationToken cancellationToken)
        {
            Shutdown();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            // No resources to dispose
        }
    }

    /// <summary>
    /// A logger provider that uses our test processor to capture logs.
    /// </summary>
    private sealed class TestLoggerProvider : ILoggerProvider
    {
        private readonly TestLogRecordProcessor _Processor;

        public TestLoggerProvider(TestLogRecordProcessor processor)
        {
            _Processor = processor;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(categoryName, _Processor);
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// A logger that creates log records and sends them to the test processor.
    /// </summary>
    private sealed class TestLogger : ILogger
    {
        private readonly string _CategoryName;
        private readonly TestLogRecordProcessor _Processor;
        private readonly InstrumentationScope _InstrumentationScope;

        public TestLogger(string categoryName, TestLogRecordProcessor processor)
        {
            _CategoryName = categoryName;
            _Processor = processor;
            _InstrumentationScope = new InstrumentationScope(categoryName);
        }

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            // For simplicity, we're not implementing scopes in this test logger
            return new NoopDisposable();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            // Create a log record
            DateTime timestamp = DateTime.UtcNow;
            string message = formatter(state, exception);

            var logRecordInfo = new LogRecordInfo(_InstrumentationScope)
            {
                Severity = ToOtelSeverity(logLevel),
                SeverityText = logLevel.ToString(),
                Body = message,
                TimestampUtc = DateTime.UtcNow,
                ObservedTimestampUtc = DateTime.UtcNow
            };

            var attributes = new List<KeyValuePair<string, object?>>();

            if (state is IEnumerable<KeyValuePair<string, object>> stateProps)
            {
                // Convert to handle nullability differences
                foreach (KeyValuePair<string, object> prop in stateProps)
                {
                    attributes.Add(new KeyValuePair<string, object?>(prop.Key, prop.Value));
                }
            }

            // Add exception information if present
            if (exception != null)
            {
                attributes.Add(new("exception.type", exception.GetType().FullName));
                attributes.Add(new("exception.message", exception.Message));
                attributes.Add(new("exception.stacktrace", exception.ToString()));
            }

            // Get trace context from Activity.Current if available
            ActivityContext spanContext = Activity.Current?.Context ?? default;
            var logRecord = new LogRecord(spanContext, in logRecordInfo)
            {
                Attributes = attributes.ToArray()
            };

            _Processor.ProcessLogRecord(logRecord);
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    // Using explicit OpenTelemetry.Logging.LogRecordInfo as parameter to avoid ambiguity
    private static OtlpLogs.LogRecord? ToOtlpLogRecord(LogRecord logRecord)
    {
        var logWriter = new OtlpLogRecordExporterAsync.OtlpLogRecordWriter();
        logWriter.WriteLogRecord(logRecord);

        using var stream = new MemoryStream(logWriter.Request.Buffer, 0, logWriter.Request.WritePosition);
        OtlpLogs.ScopeLogs scopeLogs = OtlpLogs.ScopeLogs.Parser.ParseFrom(stream);
        return scopeLogs.LogRecords.FirstOrDefault();
    }
}
