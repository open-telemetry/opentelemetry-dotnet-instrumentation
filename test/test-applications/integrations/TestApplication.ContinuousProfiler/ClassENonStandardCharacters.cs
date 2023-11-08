// <copyright file="ClassENonStandardCharacters.cs" company="OpenTelemetry Authors">
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

#pragma warning disable SA1649 // File name should match first type name
internal static class ClassENonStandardCharactersĄĘÓŁŻŹĆąęółżźśćĜЖᏳⳄʤǋₓڿଟഐቐ〣‿؁੮ᾭ_<TClass>
#pragma warning restore SA1649 // File name should match first type name
{
    public static void GenericMethodDFromGenericClass<TMethod, TMethod2>(TClass classArg, TMethod methodArg, TMethod2 additionalArg)
    {
        dynamic test = new TestDynamicClass();

        test("Param1", "Param2");
    }
}
