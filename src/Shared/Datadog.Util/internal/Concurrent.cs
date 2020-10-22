using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Datadog.Util
{
    internal static class Concurrent
    {
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
            T existingValue = Interlocked.CompareExchange(ref storageLocation, value, null);
            return existingValue ?? value;
        }
    }
}
