// <copyright file="GenericTarget.cs" company="OpenTelemetry Authors">
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

namespace TestApplication.ExampleLibrary.GenericTests;

public class GenericTarget<T1, T2>
{
    public M1 ReturnM1<M1, M2>(M1 input1, M2 input2)
    {
        return input1;
    }

    public M2 ReturnM2<M1, M2>(M1 input1, M2 input2)
    {
        return input2;
    }

    public T1 ReturnT1(object input)
    {
        return (T1)input;
    }

    public T2 ReturnT2(object input)
    {
        return (T2)input;
    }
}
