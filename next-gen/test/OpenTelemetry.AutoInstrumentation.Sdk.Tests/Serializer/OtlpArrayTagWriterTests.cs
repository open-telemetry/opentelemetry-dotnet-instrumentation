// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpenTelemetryProtocol.Serializer;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Serializer;

public sealed class OtlpArrayTagWriterTests : IDisposable
{
    private readonly ProtobufOtlpTagWriter.OtlpArrayTagWriter _ArrayTagWriter;
    private readonly ActivityListener _ActivityListener;
    private readonly ActivityIdFormat _PreviousDefaultIdFormat;
    private readonly bool _PreviousForceDefaultIdFormat;

    public OtlpArrayTagWriterTests()
    {
        // Store previous global settings
        _PreviousDefaultIdFormat = Activity.DefaultIdFormat;
        _PreviousForceDefaultIdFormat = Activity.ForceDefaultIdFormat;

        // Set test-specific global settings
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;

        _ArrayTagWriter = new ProtobufOtlpTagWriter.OtlpArrayTagWriter();
        _ActivityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
        };

        ActivitySource.AddActivityListener(_ActivityListener);
    }

    [Fact]
    public void BeginWriteArray_InitializesArrayState()
    {
        // Act
        ProtobufOtlpTagWriter.OtlpTagWriterArrayState arrayState = _ArrayTagWriter.BeginWriteArray();

        // Assert
        Assert.NotNull(arrayState.Buffer);
        Assert.Equal(0, arrayState.WritePosition);
        Assert.True(arrayState.Buffer.Length == 2048);
    }

    [Fact]
    public void WriteNullValue_AddsNullValueToBuffer()
    {
        // Arrange
        ProtobufOtlpTagWriter.OtlpTagWriterArrayState arrayState = _ArrayTagWriter.BeginWriteArray();

        // Act
        _ArrayTagWriter.WriteNullValue(ref arrayState);

        // Assert
        // Check that the buffer contains the correct tag and length for a null value
        Assert.True(arrayState.WritePosition > 0);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void WriteIntegralValue_WritesIntegralValueToBuffer(long value)
    {
        // Arrange
        ProtobufOtlpTagWriter.OtlpTagWriterArrayState arrayState = _ArrayTagWriter.BeginWriteArray();

        // Act
        _ArrayTagWriter.WriteIntegralValue(ref arrayState, value);

        // Assert
        Assert.True(arrayState.WritePosition > 0);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    public void WriteFloatingPointValue_WritesFloatingPointValueToBuffer(double value)
    {
        // Arrange
        ProtobufOtlpTagWriter.OtlpTagWriterArrayState arrayState = _ArrayTagWriter.BeginWriteArray();

        // Act
        _ArrayTagWriter.WriteFloatingPointValue(ref arrayState, value);

        // Assert
        Assert.True(arrayState.WritePosition > 0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WriteBooleanValue_WritesBooleanValueToBuffer(bool value)
    {
        // Arrange
        ProtobufOtlpTagWriter.OtlpTagWriterArrayState arrayState = _ArrayTagWriter.BeginWriteArray();

        // Act
        _ArrayTagWriter.WriteBooleanValue(ref arrayState, value);

        // Assert
        Assert.True(arrayState.WritePosition > 0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("test")]
    public void WriteStringValue_WritesStringValueToBuffer(string value)
    {
        // Arrange
        ProtobufOtlpTagWriter.OtlpTagWriterArrayState arrayState = _ArrayTagWriter.BeginWriteArray();

        // Act
        _ArrayTagWriter.WriteStringValue(ref arrayState, value.AsSpan());

        // Assert
        Assert.True(arrayState.WritePosition > 0);
    }

    [Fact]
    public void TryResize_SucceedsInitially()
    {
        // Act
        _ArrayTagWriter.BeginWriteArray();
        bool result = _ArrayTagWriter.TryResize();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TryResize_RepeatedResizingStopsAtMaxBufferSize()
    {
        // Arrange
        ProtobufOtlpTagWriter.OtlpTagWriterArrayState arrayState = _ArrayTagWriter.BeginWriteArray();
        bool resizeResult = true;

        // Act: Repeatedly attempt to resize until reaching maximum buffer size
        while (resizeResult)
        {
            resizeResult = _ArrayTagWriter.TryResize();
        }

        // Assert
        Assert.False(resizeResult, "Buffer should not resize beyond the maximum allowed size.");
    }

    public void Dispose()
    {
        // Clean up the thread buffer after each test
        ProtobufOtlpTagWriter.OtlpArrayTagWriter.s_ThreadBuffer = null;
        _ActivityListener.Dispose();

        // Restore previous global settings
        Activity.DefaultIdFormat = _PreviousDefaultIdFormat;
        Activity.ForceDefaultIdFormat = _PreviousForceDefaultIdFormat;
    }
}
