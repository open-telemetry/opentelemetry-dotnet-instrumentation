// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Metrics;

public class MetricBatchTests
{
    [Fact]
    public void WriteTo_NullWriter_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = new TestLogger();
        var resource = new Resource(new Dictionary<string, object>());
        var producers = Array.Empty<MetricProducer>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            var batch = new MetricBatch(logger, resource, producers);
            batch.WriteTo(null!);
        });
    }

    [Fact]
    public void WriteTo_ValidWriter_CallsWriterMethods()
    {
        // Arrange
        var logger = new TestLogger();
        var resource = new Resource(new Dictionary<string, object> { { "test.key", "test.value" } });
        var producer = new TestMetricProducer(true);
        var producers = new MetricProducer[] { producer };
        var writer = new TestMetricBatchWriter();

        var batch = new MetricBatch(logger, resource, producers);

        // Act
        var result = batch.WriteTo(writer);

        // Assert
        Assert.True(result);
        Assert.True(writer.BeginBatchCalled);
        Assert.True(writer.EndBatchCalled);
        Assert.Equal(resource, writer.BeginBatchResource);
        Assert.True(producer.WriteToWasCalled);
    }

    [Fact]
    public void WriteTo_ProducerThrowsException_ContinuesWithOtherProducers()
    {
        // Arrange
        var logger = new TestLogger();
        var resource = new Resource(new Dictionary<string, object>());
        var producer1 = new TestMetricProducer(false, throwException: true);
        var producer2 = new TestMetricProducer(true);
        var producers = new MetricProducer[] { producer1, producer2 };
        var writer = new TestMetricBatchWriter();

        var batch = new MetricBatch(logger, resource, producers);

        // Act
        var result = batch.WriteTo(writer);

        // Assert
        Assert.True(result);
        Assert.True(writer.BeginBatchCalled);
        Assert.True(writer.EndBatchCalled);
        Assert.True(producer1.WriteToWasCalled);
        Assert.True(producer2.WriteToWasCalled);
        Assert.True(logger.ExceptionLogged);
    }

    [Fact]
    public void WriteTo_EmptyProducersArray_ReturnsTrue()
    {
        // Arrange
        var logger = new TestLogger();
        var resource = new Resource(new Dictionary<string, object>());
        var producers = Array.Empty<MetricProducer>();
        var writer = new TestMetricBatchWriter();

        var batch = new MetricBatch(logger, resource, producers);

        // Act
        var result = batch.WriteTo(writer);

        // Assert
        Assert.True(result);
        Assert.True(writer.BeginBatchCalled);
        Assert.True(writer.EndBatchCalled);
    }

    private sealed class TestLogger : ILogger
    {
        public bool ExceptionLogged { get; private set; }

        IDisposable? ILogger.BeginScope<TState>(TState state) => null;

        bool ILogger.IsEnabled(LogLevel logLevel) => true;

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (exception != null)
            {
                ExceptionLogged = true;
            }
        }
    }

    private sealed class TestMetricProducer : MetricProducer
    {
        private readonly bool _ReturnValue;
        private readonly bool _ThrowException;

        public TestMetricProducer(bool returnValue, bool throwException = false)
        {
            _ReturnValue = returnValue;
            _ThrowException = throwException;
        }

        public bool WriteToWasCalled { get; private set; }

        public override bool WriteTo(MetricWriter writer)
        {
            WriteToWasCalled = true;
            if (_ThrowException)
            {
                throw new InvalidOperationException("Test exception");
            }

            return _ReturnValue;
        }
    }

    private sealed class TestMetricBatchWriter : MetricBatchWriter
    {
        public bool BeginBatchCalled { get; private set; }

        public Resource? BeginBatchResource { get; private set; }

        public bool EndBatchCalled { get; private set; }

        public override void BeginBatch(Resource resource)
        {
            BeginBatchCalled = true;
            BeginBatchResource = resource;
            base.BeginBatch(resource);
        }

        public override void EndBatch()
        {
            EndBatchCalled = true;
            base.EndBatch();
        }
    }
}
