using System;

namespace IntegrationTests.Helpers.Constants;

internal static class TimeConstants
{
    public const long NanoSecondsPerTick = 1000000 / TimeSpan.TicksPerMillisecond;

    public const long UnixEpochInTicks = 621355968000000000; // = DateTimeOffset.FromUnixTimeMilliseconds(0).Ticks
}
