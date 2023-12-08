// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace IntegrationTests.Helpers;

internal static class TimeExtensions
{
    private const long NanoSecondsPerTick = 1000000 / TimeSpan.TicksPerMillisecond;

    private const long UnixEpochInTicks = 621355968000000000; // = DateTimeOffset.FromUnixTimeMilliseconds(0).Ticks

    /// <summary>
    /// Returns the number of nanoseconds that have elapsed since 1970-01-01T00:00:00.000Z.
    /// </summary>
    /// <param name="dateTimeOffset">The value to get the number of elapsed nanoseconds for.</param>
    /// <returns>The number of nanoseconds that have elapsed since 1970-01-01T00:00:00.000Z.</returns>
    public static long ToUnixTimeNanoseconds(this DateTimeOffset dateTimeOffset)
    {
        return (dateTimeOffset.Ticks - UnixEpochInTicks) * NanoSecondsPerTick;
    }

    /// <summary>
    /// Reconverts a long timestamp from TimeExtensions.ToUnixTimeMicroseconds.
    /// </summary>
    /// <param name="ts">The unix time in microseconds.</param>
    /// <returns>The corresponding DateTimeOffset.</returns>
    public static DateTimeOffset UnixMicrosecondsToDateTimeOffset(this long ts)
    {
        const long microsecondsPerMillisecond = 1000;
        const long ticksPerMicrosecond = TimeSpan.TicksPerMillisecond / microsecondsPerMillisecond;
        var ticks = (ts * ticksPerMicrosecond) + UnixEpochInTicks;
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }
}
