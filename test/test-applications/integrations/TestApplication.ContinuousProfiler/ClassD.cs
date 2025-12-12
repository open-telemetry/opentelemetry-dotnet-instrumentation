// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

namespace My.Custom.Test.Namespace;

internal static class ClassD<T01, T02, T03, T04, T05, T06, T07, T08, T09, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>
{
    // Always on profiler supports fetching at most 20 generic arguments. This class covers scenario where there are more than 20 parameters.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void MethodD(T01 p01, T02 p02, T03 p03, T04 p04, T05 p05, T06 p06, T07 p07, T08 p08, T09 p09, T10 p10, T11 p11, T12 p12, T13 p13, T14 p14, T15 p15, T16 p16, T17 p17, T18 p18, T19 p19, T20 p20, T21 p21)
    {
        ClassENonStandardCharactersĄĘÓŁŻŹĆąęółżźśćĜЖᏳⳄʤǋₓڿଟഐቐ〣‿؁੮ᾭ_<TimeSpan>.GenericMethodDFromGenericClass(TimeSpan.MaxValue, p01, 1);
    }
}
