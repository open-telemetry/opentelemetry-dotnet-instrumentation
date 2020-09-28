using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace OpenTelemetry.Util
{
    internal static class Concurrent
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SetOrGetRaceWinner<T>(ref T storageLocation, T value) where T : class
        {
            T existingValue = Interlocked.CompareExchange(ref storageLocation, value, null);
            return existingValue ?? value;
        }
    }
}
