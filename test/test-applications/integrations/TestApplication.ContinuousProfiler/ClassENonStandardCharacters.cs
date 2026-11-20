// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace My.Custom.Test.Namespace;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1317 // Non-Latin characters exercise profiler stack frame names.
internal static class ClassENonStandardCharactersĄĘÓŁŻŹĆąęółżźśćĜЖᏳⳄʤǋₓڿଟഐቐ〣‿؁੮ᾭ_<TClass>
#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1317 // Non-Latin characters exercise profiler stack frame names.
{
    public static void GenericMethodDFromGenericClass<TMethod, TMethod2>(TClass classArg, TMethod methodArg, TMethod2 additionalArg)
    {
        dynamic test = new TestDynamicClass();

        test("Param1", "Param2");
    }
}
