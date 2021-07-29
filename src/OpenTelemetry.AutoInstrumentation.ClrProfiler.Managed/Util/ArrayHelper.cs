using System;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Util
{
    internal static class ArrayHelper
    {
        public static T[] Empty<T>()
        {
            return Array.Empty<T>();
        }

        public static T[] Concat<T>(this T[] array, params T[] newElements)
        {
            var destination = new T[array.Length + newElements.Length];

            Array.Copy(array, 0, destination, 0, array.Length);
            Array.Copy(newElements, 0, destination, array.Length, newElements.Length);

            return destination;
        }
    }
}
