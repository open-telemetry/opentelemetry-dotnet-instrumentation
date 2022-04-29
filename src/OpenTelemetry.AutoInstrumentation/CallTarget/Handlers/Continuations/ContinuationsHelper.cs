// <copyright file="ContinuationsHelper.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers.Continuations;

internal static class ContinuationsHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Type GetResultType(Type parentType)
    {
        Type currentType = parentType;
        while (currentType != null)
        {
            Type[] typeArguments = currentType.GenericTypeArguments ?? Type.EmptyTypes;
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

#if NETCOREAPP3_1_OR_GREATER
#else
    internal static TTo Convert<TFrom, TTo>(TFrom value)
    {
        return Converter<TFrom, TTo>.Convert(value);
    }

    private static class Converter<TFrom, TTo>
    {
        private static readonly ConvertDelegate _converter;

        static Converter()
        {
            DynamicMethod dMethod = new DynamicMethod($"Converter<{typeof(TFrom).Name},{typeof(TTo).Name}>", typeof(TTo), new[] { typeof(TFrom) }, typeof(ConvertDelegate).Module, true);
            ILGenerator il = dMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ret);
            _converter = (ConvertDelegate)dMethod.CreateDelegate(typeof(ConvertDelegate));
        }

        private delegate TTo ConvertDelegate(TFrom value);

        public static TTo Convert(TFrom value)
        {
            return _converter(value);
        }
    }
#endif
}
