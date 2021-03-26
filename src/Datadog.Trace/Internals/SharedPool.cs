#if NETSTANDARD2_0 || NETCOREAPP3_1_OR_GREATER
using System.Buffers;
#endif

namespace Datadog.Trace.Internals
{
    internal static class SharedPool<T>
    {
        public static T[] Rent(int minimumLength)
        {
#if NETSTANDARD2_0 || NETCOREAPP3_1_OR_GREATER
            return ArrayPool<T>.Shared.Rent(minimumLength);
#else
            // If shared pool is not supported
            // allocate new array anywhere

            return new T[minimumLength];
#endif
        }

        public static void Return(T[] array)
        {
            // If shared pool is not supported
            // do nothing, there is nothing to return

#if NETSTANDARD2_0 || NETCOREAPP3_1_OR_GREATER
            ArrayPool<T>.Shared.Return(array);
#endif
        }
    }
}
