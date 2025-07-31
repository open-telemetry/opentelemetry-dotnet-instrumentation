// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Tracing;

using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Tracing;

public sealed class SpanInfoTests
{
    [Fact]
    public void Constructor_WithValidParameters_SetsProperties()
    {
        // Arrange
        var scope = new InstrumentationScope("test-scope");
        const string spanName = "test-span";

        // Act
        var spanInfo = new SpanInfo(scope, spanName)
        {
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.None,
            StartTimestampUtc = DateTime.UtcNow,
            EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(100)
        };

        // Assert
        Assert.Equal(scope, spanInfo.Scope);
        Assert.Equal(spanName, spanInfo.Name);
        Assert.Null(spanInfo.Kind);
        Assert.Equal(ActivityStatusCode.Unset, spanInfo.StatusCode);
        Assert.Null(spanInfo.StatusDescription);
        Assert.Null(spanInfo.TraceState);
        Assert.Equal(default(ActivitySpanId), spanInfo.ParentSpanId);
    }

    [Fact]
    public void Constructor_WithNullScope_ThrowsArgumentNullException()
    {
        // Arrange
        const string spanName = "test-span";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SpanInfo(null!, spanName)
        {
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.None,
            StartTimestampUtc = DateTime.UtcNow,
            EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(100)
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidSpanName_ThrowsArgumentException(string? invalidName)
    {
        // Arrange
        var scope = new InstrumentationScope("test-scope");

        // Act & Assert
        if (invalidName == null)
        {
            Assert.Throws<ArgumentNullException>(() => new SpanInfo(scope, invalidName!)
            {
                TraceId = ActivityTraceId.CreateRandom(),
                SpanId = ActivitySpanId.CreateRandom(),
                TraceFlags = ActivityTraceFlags.None,
                StartTimestampUtc = DateTime.UtcNow,
                EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(100)
            });
        }
        else
        {
            Assert.Throws<ArgumentException>(() => new SpanInfo(scope, invalidName!)
            {
                TraceId = ActivityTraceId.CreateRandom(),
                SpanId = ActivitySpanId.CreateRandom(),
                TraceFlags = ActivityTraceFlags.None,
                StartTimestampUtc = DateTime.UtcNow,
                EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(100)
            });
        }
    }

    [Fact]
    public void InitProperties_WithValidValues_SetsCorrectly()
    {
        // Arrange
        var scope = new InstrumentationScope("test-scope");
        const string spanName = "test-span";
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var parentSpanId = ActivitySpanId.CreateRandom();
        var traceFlags = ActivityTraceFlags.Recorded;
        const string traceState = "key=value";
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddMilliseconds(100);

        // Act
        var spanInfo = new SpanInfo(scope, spanName)
        {
            Kind = ActivityKind.Client,
            TraceId = traceId,
            SpanId = spanId,
            ParentSpanId = parentSpanId,
            TraceFlags = traceFlags,
            TraceState = traceState,
            StartTimestampUtc = startTime,
            EndTimestampUtc = endTime,
            StatusCode = ActivityStatusCode.Ok,
            StatusDescription = "Success"
        };

        // Assert
        Assert.Equal(ActivityKind.Client, spanInfo.Kind);
        Assert.Equal(traceId, spanInfo.TraceId);
        Assert.Equal(spanId, spanInfo.SpanId);
        Assert.Equal(parentSpanId, spanInfo.ParentSpanId);
        Assert.Equal(traceFlags, spanInfo.TraceFlags);
        Assert.Equal(traceState, spanInfo.TraceState);
        Assert.Equal(startTime, spanInfo.StartTimestampUtc);
        Assert.Equal(endTime, spanInfo.EndTimestampUtc);
        Assert.Equal(ActivityStatusCode.Ok, spanInfo.StatusCode);
        Assert.Equal("Success", spanInfo.StatusDescription);
    }

    [Fact]
    public void StartTimestampUtc_WithNonUtcDateTime_ConvertsToUtc()
    {
        // Arrange
        var scope = new InstrumentationScope("test-scope");
        const string spanName = "test-span";
        var localTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var expectedUtcTime = localTime.ToUniversalTime();

        // Act
        var spanInfo = new SpanInfo(scope, spanName)
        {
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.None,
            StartTimestampUtc = localTime,
            EndTimestampUtc = localTime.AddMilliseconds(100)
        };

        // Assert
        Assert.Equal(expectedUtcTime, spanInfo.StartTimestampUtc);
        Assert.Equal(DateTimeKind.Utc, spanInfo.StartTimestampUtc.Kind);
    }

    [Fact]
    public void EndTimestampUtc_WithNonUtcDateTime_ConvertsToUtc()
    {
        // Arrange
        var scope = new InstrumentationScope("test-scope");
        const string spanName = "test-span";
        var localTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var expectedUtcTime = localTime.ToUniversalTime();

        // Act
        var spanInfo = new SpanInfo(scope, spanName)
        {
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.None,
            StartTimestampUtc = localTime,
            EndTimestampUtc = localTime.AddMilliseconds(100)
        };

        // Assert
        Assert.Equal(expectedUtcTime.AddMilliseconds(100), spanInfo.EndTimestampUtc);
        Assert.Equal(DateTimeKind.Utc, spanInfo.EndTimestampUtc.Kind);
    }

    [Fact]
    public void SpanInfo_IsRecord_SupportsValueEquality()
    {
        // Arrange
        var scope = new InstrumentationScope("test-scope");
        const string spanName = "test-span";
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var traceFlags = ActivityTraceFlags.Recorded;
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddMilliseconds(100);

        var spanInfo1 = new SpanInfo(scope, spanName)
        {
            TraceId = traceId,
            SpanId = spanId,
            TraceFlags = traceFlags,
            StartTimestampUtc = startTime,
            EndTimestampUtc = endTime
        };

        var spanInfo2 = new SpanInfo(scope, spanName)
        {
            TraceId = traceId,
            SpanId = spanId,
            TraceFlags = traceFlags,
            StartTimestampUtc = startTime,
            EndTimestampUtc = endTime
        };

        // Act & Assert
        Assert.Equal(spanInfo1, spanInfo2);
        Assert.True(spanInfo1 == spanInfo2);
        Assert.False(spanInfo1 != spanInfo2);
        Assert.Equal(spanInfo1.GetHashCode(), spanInfo2.GetHashCode());
    }
}
