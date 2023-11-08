// <copyright file="GenericClassC.cs" company="OpenTelemetry Authors">
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

namespace My.Custom.Test.Namespace;

internal static class GenericClassC<T>
{
    public static int GenericMethodCFromGenericClass(T arg)
    {
        GenericMethodCFromGenericClass(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21);
        return 1;
    }

    public static void GenericMethodCFromGenericClass<T01, T02, T03, T04, T05, T06, T07, T08, T09, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(T01 p01, T02 p02, T03 p03, T04 p04, T05 p05, T06 p06, T07 p07, T08 p08, T09 p09, T10 p10, T11 p11, T12 p12, T13 p13, T14 p14, T15 p15, T16 p16, T17 p17, T18 p18, T19 p19, T20 p20, T21 p21)
    {
        // Always on profiler supports fetching at most 20 generic arguments. This method covers scenario where there are more than 20 parameters.
        ClassD<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>.MethodD(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21);
    }
}
