using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Datadog.Util
{
    internal static class Concurrent
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TrySetIfNull<T>(ref T storageLocation, T value) where T : class
        {
            T prevValue = Interlocked.CompareExchange(ref storageLocation, value, null);
            return Object.ReferenceEquals(null, prevValue);
        }

        /// <summary>
        /// If <c>storageLocation</c> is <c>null</c>, sets the contents of <c>storageLocation</c> to <c>value</c>;
        /// otherwise keeps the previous contents (in an atomic, thread-safe manner). 
        /// Returns the contents of <c>storageLocation</c> after the operation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="storageLocation"></param>
        /// <param name="value"></param>
        /// <returns>If <c>storageLocation</c> was not <c>null</c>, the original value of <c>storageLocation</c>
        /// (which is unchanged in that case);
        /// otherwise, the new value of <c>storageLocation</c> (which, in that case, is the same as the
        /// specified <c>value</c> parameter).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T TrySetOrGetValue<T>(ref T storageLocation, T value) where T : class
        {
            return CompareExchangeResult(ref storageLocation, value, null);
        }

        /// <summary>
        /// Is like <c>Interlocked.CompareExchange(..)</c>, but returns the final/resulting value,
        /// instead of the previous/original value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="storageLocation"></param>
        /// <param name="value"></param>
        /// <param name="comparand"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T CompareExchangeResult<T>(ref T storageLocation, T value,T comparand) where T : class
        {
            T prevValue = Interlocked.CompareExchange(ref storageLocation, value, comparand);
            return Object.ReferenceEquals(prevValue, comparand) ? value : prevValue;
        }
    }
}
