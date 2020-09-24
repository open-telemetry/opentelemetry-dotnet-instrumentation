using System;

namespace OpenTelemetry.Util
{
    internal static class ArrayExtensions
    {
        public static bool IsEqual<T>(this T[] arr1, T[] arr2) where T : IEquatable<T>
        {
            if (arr1 == arr2)
            {
                return true;
            }

            if (arr1 == null || arr2 == null || arr1.Length != arr2.Length)
            {
                return false;
            }

            for (int i = arr1.Length - 1; i >= 0; i--)
            {
                if (false == arr1[i].Equals(arr2[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
