// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers.Continuations;

internal static class ContinuationsHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Type GetResultType(Type parentType)
    {
        var currentType = parentType;
        while (currentType != null)
        {
            var typeArguments = currentType.GenericTypeArguments ?? Type.EmptyTypes;
            switch (typeArguments.Length)
            {
                case 0:
                    return typeof(object);
                case 1:
                    return typeArguments[0];
                default:
                    currentType = currentType.BaseType;
                    break;
            }
        }

        return typeof(object);
    }

#if NETFRAMEWORK
    internal static TTo Convert<TFrom, TTo>(TFrom value)
    {
        return Converter<TFrom, TTo>.Convert(value);
    }

    private static class Converter<TFrom, TTo>
    {
        private static readonly ConvertDelegate Instance = CreateConverter();

        private delegate TTo ConvertDelegate(TFrom value);

        public static TTo Convert(TFrom value)
        {
            return Instance(value);
        }

        private static ConvertDelegate CreateConverter()
        {
            var dMethod = new DynamicMethod($"Converter<{typeof(TFrom).Name},{typeof(TTo).Name}>", typeof(TTo), [typeof(TFrom)], typeof(ConvertDelegate).Module, true);
            var il = dMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ret);
            return (ConvertDelegate)dMethod.CreateDelegate(typeof(ConvertDelegate));
        }
    }
#endif
}
