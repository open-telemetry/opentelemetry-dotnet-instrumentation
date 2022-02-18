using System;

namespace OpenTelemetry.AutoInstrumentation.Util;

internal static class ArrayHelper
{
    public static T[] Concat<T>(this T[] array, params T[] newElements)
    {
        var destination = new T[array.Length + newElements.Length];

        Array.Copy(array, 0, destination, 0, array.Length);
        Array.Copy(newElements, 0, destination, array.Length, newElements.Length);

        return destination;
    }
}
