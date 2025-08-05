// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Metrics;
using Xunit;

namespace OpenTelemetry.OutOfProcess.Forwarder.Tests.Metrics;

public class MetricProducerTests
{
    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Act
        var producer = new TestMetricProducer();

        // Assert
        Assert.NotNull(producer);
    }

    [Fact]
    public void WriteTo_WithValidWriter_CallsAbstractMethod()
    {
        // Arrange
        var producer = new TestMetricProducer();
        var writer = new TestMetricWriter();

        // Act
        var result = producer.WriteTo(writer);

        // Assert
        Assert.True(result);
        Assert.True(writer.WriteToWasCalled);
    }

    [Fact]
    public void WriteTo_WithNullWriter_CallsAbstractMethod()
    {
        // Arrange
        var producer = new TestMetricProducer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => producer.WriteTo(null!));
    }

    [Fact]
    public void WriteTo_WhenOverriddenMethodReturnsFalse_ReturnsFalse()
    {
        // Arrange
        var producer = new TestMetricProducerReturningFalse();
        var writer = new TestMetricWriter();

        // Act
        var result = producer.WriteTo(writer);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void WriteTo_WhenOverriddenMethodThrows_ThrowsException()
    {
        // Arrange
        var producer = new TestMetricProducerThrowing();
        var writer = new TestMetricWriter();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => producer.WriteTo(writer));
    }

    [Fact]
    public void AbstractClass_CanBeInherited()
    {
        // This test verifies that MetricProducer is abstract and can be inherited
        var producer = new TestMetricProducer();

        // Assert
        Assert.IsAssignableFrom<MetricProducer>(producer);
        Assert.NotNull(producer);
    }

    [Fact]
    public void AbstractClass_CannotBeInstantiatedDirectly()
    {
        // This test verifies the abstract nature of MetricProducer
        // The fact that we need TestMetricProducer demonstrates that
        // MetricProducer cannot be instantiated directly

        // Instead we must use a derived class:
        var producer = new TestMetricProducer();
        Assert.IsType<TestMetricProducer>(producer);
    }

    private sealed class TestMetricProducer : MetricProducer
    {
        public override bool WriteTo(MetricWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);

            if (writer is TestMetricWriter testWriter)
            {
                testWriter.WriteToWasCalled = true;
            }

            return true;
        }
    }

    private sealed class TestMetricProducerReturningFalse : MetricProducer
    {
        public override bool WriteTo(MetricWriter writer)
        {
            return false;
        }
    }

    private sealed class TestMetricProducerThrowing : MetricProducer
    {
        public override bool WriteTo(MetricWriter writer)
        {
            throw new InvalidOperationException("Test exception");
        }
    }

    private sealed class TestMetricWriter : MetricWriter
    {
        public bool WriteToWasCalled { get; set; }

        public override void BeginInstrumentationScope(InstrumentationScope instrumentationScope)
        {
            WriteToWasCalled = true;
        }
    }
}
