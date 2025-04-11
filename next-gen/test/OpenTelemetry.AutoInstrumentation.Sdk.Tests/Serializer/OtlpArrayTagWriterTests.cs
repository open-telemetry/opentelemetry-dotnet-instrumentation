// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpenTelemetryProtocol.Serializer;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Serializer;

public sealed class OtlpArrayTagWriterTests : IDisposable
{
    static OtlpArrayTagWriterTests()
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;
    }

    private readonly ProtobufOtlpTagWriter.OtlpArrayTagWriter arrayTagWriter;
    private readonly ActivityListener activityListener;

    public OtlpArrayTagWriterTests()
    {
        this.arrayTagWriter = new ProtobufOtlpTagWriter.OtlpArrayTagWriter();
        this.activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
        };

        ActivitySource.AddActivityListener(this.activityListener);
    }

    [Fact]
    public void BeginWriteArray_InitializesArrayState()
    {
        // Act
        var arrayState = this.arrayTagWriter.BeginWriteArray();

        // Assert
        Assert.NotNull(arrayState.Buffer);
        Assert.Equal(0, arrayState.WritePosition);
        Assert.True(arrayState.Buffer.Length == 2048);
    }

    [Fact]
    public void WriteNullValue_AddsNullValueToBuffer()
    {
        // Arrange
        var arrayState = this.arrayTagWriter.BeginWriteArray();

        // Act
        this.arrayTagWriter.WriteNullValue(ref arrayState);

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
        var arrayState = this.arrayTagWriter.BeginWriteArray();

        // Act
        this.arrayTagWriter.WriteIntegralValue(ref arrayState, value);

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
        var arrayState = this.arrayTagWriter.BeginWriteArray();

        // Act
        this.arrayTagWriter.WriteFloatingPointValue(ref arrayState, value);

        // Assert
        Assert.True(arrayState.WritePosition > 0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WriteBooleanValue_WritesBooleanValueToBuffer(bool value)
    {
        // Arrange
        var arrayState = this.arrayTagWriter.BeginWriteArray();

        // Act
        this.arrayTagWriter.WriteBooleanValue(ref arrayState, value);

        // Assert
        Assert.True(arrayState.WritePosition > 0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("test")]
    public void WriteStringValue_WritesStringValueToBuffer(string value)
    {
        // Arrange
        var arrayState = this.arrayTagWriter.BeginWriteArray();

        // Act
        this.arrayTagWriter.WriteStringValue(ref arrayState, value.AsSpan());

        // Assert
        Assert.True(arrayState.WritePosition > 0);
    }

    [Fact]
    public void TryResize_SucceedsInitially()
    {
        // Act
        this.arrayTagWriter.BeginWriteArray();
        bool result = this.arrayTagWriter.TryResize();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TryResize_RepeatedResizingStopsAtMaxBufferSize()
    {
        // Arrange
        var arrayState = this.arrayTagWriter.BeginWriteArray();
        bool resizeResult = true;

        // Act: Repeatedly attempt to resize until reaching maximum buffer size
        while (resizeResult)
        {
            resizeResult = this.arrayTagWriter.TryResize();
        }

        // Assert
        Assert.False(resizeResult, "Buffer should not resize beyond the maximum allowed size.");
    }

    public void Dispose()
    {
        // Clean up the thread buffer after each test
        ProtobufOtlpTagWriter.OtlpArrayTagWriter.ThreadBuffer = null;
        this.activityListener.Dispose();
    }
}
