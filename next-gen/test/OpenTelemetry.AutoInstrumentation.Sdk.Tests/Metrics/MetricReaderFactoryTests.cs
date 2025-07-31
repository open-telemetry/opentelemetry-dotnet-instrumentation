// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable CA1852 // Type can be sealed

using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Metrics;

public class MetricReaderFactoryTests
{
    [Fact]
    public void CreatePeriodicExportingMetricReaderAsync_ValidArguments_ReturnsMetricReader()
    {
        // Arrange
        var loggerFactory = new TestLoggerFactory();
        var resource = new Resource(new Dictionary<string, object> { { "test.key", "test.value" } });
        var exporter = new TestMetricExporterAsync();
        var options = new PeriodicExportingMetricReaderOptions();

        // Act
        var reader = MetricReaderFactory.CreatePeriodicExportingMetricReaderAsync(
            loggerFactory,
            resource,
            exporter,
            null,
            options);

        // Assert
        Assert.NotNull(reader);
        Assert.IsAssignableFrom<IMetricReader>(reader);
    }

    [Fact]
    public void CreatePeriodicExportingMetricReaderAsync_NullLoggerFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var resource = new Resource(new Dictionary<string, object>());
        var exporter = new TestMetricExporterAsync();
        var options = new PeriodicExportingMetricReaderOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            MetricReaderFactory.CreatePeriodicExportingMetricReaderAsync(
                null!,
                resource,
                exporter,
                null,
                options));
    }

    [Fact]
    public void CreatePeriodicExportingMetricReaderAsync_NullExporter_ThrowsArgumentNullException()
    {
        // Arrange
        var loggerFactory = new TestLoggerFactory();
        var resource = new Resource(new Dictionary<string, object>());
        var options = new PeriodicExportingMetricReaderOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            MetricReaderFactory.CreatePeriodicExportingMetricReaderAsync(
                loggerFactory,
                resource,
                null!,
                null,
                options));
    }

    [Fact]
    public void CreatePeriodicExportingMetricReaderAsync_WithMetricProducerFactories_CreatesReader()
    {
        // Arrange
        var loggerFactory = new TestLoggerFactory();
        var resource = new Resource(new Dictionary<string, object>());
        var exporter = new TestMetricExporterAsync();
        var options = new PeriodicExportingMetricReaderOptions();
        var producerFactories = new List<IMetricProducerFactory> { new TestMetricProducerFactory() };

        // Act
        var reader = MetricReaderFactory.CreatePeriodicExportingMetricReaderAsync(
            loggerFactory,
            resource,
            exporter,
            producerFactories,
            options);

        // Assert
        Assert.NotNull(reader);
        Assert.IsAssignableFrom<IMetricReader>(reader);
    }

    [Fact]
    public void CreatePeriodicExportingMetricReaderAsync_WithCustomOptions_CreatesReader()
    {
        // Arrange
        var loggerFactory = new TestLoggerFactory();
        var resource = new Resource(new Dictionary<string, object>());
        var exporter = new TestMetricExporterAsync();
        var options = new PeriodicExportingMetricReaderOptions(
            AggregationTemporality.Delta,
            2000,
            15000);

        // Act
        var reader = MetricReaderFactory.CreatePeriodicExportingMetricReaderAsync(
            loggerFactory,
            resource,
            exporter,
            null,
            options);

        // Assert
        Assert.NotNull(reader);
        Assert.IsAssignableFrom<IMetricReader>(reader);
    }

    private class TestLoggerFactory : ILoggerFactory
    {
        void ILoggerFactory.AddProvider(ILoggerProvider provider)
        {
        }

        ILogger ILoggerFactory.CreateLogger(string categoryName) => new TestLogger();

        void IDisposable.Dispose()
        {
        }
    }

    private class TestLogger : ILogger
    {
        IDisposable? ILogger.BeginScope<TState>(TState state) => null;

        bool ILogger.IsEnabled(LogLevel logLevel) => true;

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }
    }

    private class TestMetricExporterAsync : IMetricExporterAsync
    {
        public bool SupportsDeltaAggregationTemporality => true;

        Task<bool> IExporterAsync<MetricBatchWriter>.ExportAsync<TBatch>(in TBatch batch, CancellationToken cancellationToken) => Task.FromResult(true);

        void IDisposable.Dispose()
        {
        }
    }

    private class TestMetricProducerFactory : IMetricProducerFactory
    {
        public MetricProducer Create(MetricProducerOptions options) => new TestMetricProducer();
    }

    private class TestMetricProducer : MetricProducer
    {
        public override bool WriteTo(MetricWriter writer) => true;
    }
}
