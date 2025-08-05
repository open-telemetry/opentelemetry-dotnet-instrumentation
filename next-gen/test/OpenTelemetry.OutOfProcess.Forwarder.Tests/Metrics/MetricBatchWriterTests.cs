// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Xunit;

namespace OpenTelemetry.OutOfProcess.Forwarder.Tests.Metrics;

public class MetricBatchWriterTests
{
    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Act
        var writer = new TestMetricBatchWriter();

        // Assert
        Assert.NotNull(writer);
        Assert.IsAssignableFrom<MetricWriter>(writer);
        Assert.IsAssignableFrom<IBatchWriter>(writer);
    }

    [Fact]
    public void BeginBatch_DefaultImplementation_DoesNotThrow()
    {
        // Arrange
        var writer = new TestMetricBatchWriter();
        var resource = new Resource(new Dictionary<string, object>());

        // Act & Assert
        var exception = Record.Exception(() => writer.BeginBatch(resource));
        Assert.Null(exception);
    }

    [Fact]
    public void EndBatch_DefaultImplementation_DoesNotThrow()
    {
        // Arrange
        var writer = new TestMetricBatchWriter();

        // Act & Assert
        var exception = Record.Exception(() => writer.EndBatch());
        Assert.Null(exception);
    }

    [Fact]
    public void BeginBatch_CanBeOverridden()
    {
        // Arrange
        var writer = new TestMetricBatchWriterWithOverrides();
        var resource = new Resource(new Dictionary<string, object>());

        // Act
        writer.BeginBatch(resource);

        // Assert
        Assert.True(writer.BeginBatchCalled);
        Assert.Equal(resource, writer.BeginBatchResource);
    }

    [Fact]
    public void EndBatch_CanBeOverridden()
    {
        // Arrange
        var writer = new TestMetricBatchWriterWithOverrides();

        // Act
        writer.EndBatch();

        // Assert
        Assert.True(writer.EndBatchCalled);
    }

    private sealed class TestMetricBatchWriter : MetricBatchWriter
    {
    }

    private sealed class TestMetricBatchWriterWithOverrides : MetricBatchWriter
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
