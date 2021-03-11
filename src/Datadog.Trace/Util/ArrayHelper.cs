using System;

namespace Datadog.Trace.Util
{
    internal static class ArrayHelper
    {
        public static T[] Empty<T>()
        {
#if NET45
            return EmptyArray<T>.Value;
#else
            return Array.Empty<T>();
#endif
        }

        public static T[] Copy<T>(T[] array)
        {
            int length = array.Length;

            return Copy(array, length, length);
        }

        public static T[] Copy<T>(T[] array, int newSize, int length)
        {
            var target = new T[newSize];
            Copy(array, target, length);

            return target;
        }

        public static void Copy<T>(T[] source, T[] target, int length)
        {
            Copy(source, target, 0, 0, length);
        }

        public static void Copy<T>(T[] source, T[] target, int sourcOffset, int targetOffset, int length)
        {
            if (typeof(T).IsPrimitive)
            {
#if NETCOREAPP3_1_OR_GREATER
                var span = source.AsSpan(sourcOffset, length);
                span.CopyTo(new Span<T>(target, targetOffset, length));
#else
                Buffer.BlockCopy(source, sourcOffset, target, targetOffset, length);
#endif
            }
            else
            {
                Array.Copy(source, sourcOffset, target, targetOffset, length);
            }
        }

        public static void Copy<T>(T[] source, T[] target)
        {
            Copy(source, target, source.Length);
        }

#if NET45
        private static class EmptyArray<T>
        {
            internal static readonly T[] Value = new T[0];
        }
#endif
    }
}
