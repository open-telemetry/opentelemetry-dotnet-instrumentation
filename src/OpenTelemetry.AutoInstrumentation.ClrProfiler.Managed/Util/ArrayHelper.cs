using System;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Util
{
    internal static class ArrayHelper
    {
        public static T[] Empty<T>()
        {
#if NET452
            return EmptyArray<T>.Value;
#else
            return Array.Empty<T>();
#endif
        }

        public static T[] Concat<T>(this T[] array, params T[] newElements)
        {
            var destination = new T[array.Length + newElements.Length];

            Array.Copy(array, 0, destination, 0, array.Length);
            Array.Copy(newElements, 0, destination, array.Length, newElements.Length);

            return destination;
        }

#if NET452
        private static class EmptyArray<T>
        {
            internal static readonly T[] Value = new T[0];
        }
#endif
    }
}
