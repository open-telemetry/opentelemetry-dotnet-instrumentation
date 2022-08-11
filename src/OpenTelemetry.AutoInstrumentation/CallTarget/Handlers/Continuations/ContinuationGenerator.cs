// <copyright file="ContinuationGenerator.cs" company="OpenTelemetry Authors">
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
using System.Runtime.CompilerServices;

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers.Continuations;

internal class ContinuationGenerator<TTarget, TReturn>
{
    public virtual TReturn SetContinuation(TTarget instance, TReturn returnValue, Exception exception, CallTargetState state)
    {
        return returnValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static TReturn ToTReturn<TFrom>(TFrom returnValue)
    {
#if NET6_0_OR_GREATER
        return Unsafe.As<TFrom, TReturn>(ref returnValue);
#else
        return ContinuationsHelper.Convert<TFrom, TReturn>(returnValue);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static TTo FromTReturn<TTo>(TReturn returnValue)
    {
#if NET6_0_OR_GREATER
        return Unsafe.As<TReturn, TTo>(ref returnValue);
#else
        return ContinuationsHelper.Convert<TReturn, TTo>(returnValue);
#endif
    }
}
