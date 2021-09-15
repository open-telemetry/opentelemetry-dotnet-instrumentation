using System;

namespace Datadog.Trace.TestHelpers
{
    internal static class TimeHelpers
    {
        /// <summary>
        /// Reconverts a long timestamp from TimeExtensions.ToUnixTimeMicroseconds.
        /// </summary>
        /// <param name="ts">The unix time in microseconds.</param>
        /// <returns>The corresponding DateTimeOffset.</returns>
        public static DateTimeOffset UnixMicrosecondsToDateTimeOffset(long ts)
        {
            const long microsecondsPerMillisecond = 1000;
            const long ticksPerMicrosecond = TimeSpan.TicksPerMillisecond / microsecondsPerMillisecond;
            var ticks = (ts * ticksPerMicrosecond) + TimeConstants.UnixEpochInTicks;
            return new DateTimeOffset(ticks, TimeSpan.Zero);
        }
    }
}
