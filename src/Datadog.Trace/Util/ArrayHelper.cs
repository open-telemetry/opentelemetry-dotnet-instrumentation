// <copyright file="ArrayHelper.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

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
            Copy(source, 0, target, 0, length);
        }

        public static void Copy<T>(T[] source, int sourceOffset, T[] target, int targetOffset, int length)
        {
            if (typeof(T).IsPrimitive)
            {
#if NETCOREAPP3_1_OR_GREATER
                var span = source.AsSpan(sourceOffset, length);
                span.CopyTo(new Span<T>(target, targetOffset, length));
#else
                Buffer.BlockCopy(source, sourceOffset, target, targetOffset, length);
#endif
            }
            else
            {
                Array.Copy(source, sourceOffset, target, targetOffset, length);
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
