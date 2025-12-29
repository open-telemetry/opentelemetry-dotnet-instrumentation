// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace My.Custom.Test.Namespace;

internal static class ClassA
{
    private static readonly Callback TestCallback = GenericClassC<int>.GenericMethodCFromGenericClass;

    private delegate int Callback(int n);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void MethodA()
    {
        const int numberOfItems = 1024;
        var items = new List<string>();
        for (var i = 0; i < numberOfItems; i++)
        {
            items.Add(i.ToString("D10000", CultureInfo.InvariantCulture));
        }

        MethodABytes(
            false,
            '\0',
            sbyte.MaxValue,
            byte.MaxValue);

        for (var i = 0; i < numberOfItems; i++)
        {
            TextWriter.Null.Write(items[i][items[i].Length - 2]);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void MethodABytes(
        bool b,
        char c,
        sbyte sb,
        byte b2)
    {
        MethodAInts(
            ushort.MaxValue,
            short.MaxValue,
            uint.MaxValue,
            int.MaxValue,
            ulong.MaxValue,
            long.MaxValue,
#if NET
            nint.MaxValue,
            nuint.MaxValue);
#else
            0x7fffffff,
            0xffffffff);
#endif
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void MethodAInts(
        ushort ui16,
        short i16,
        uint ui32,
        int i32,
        ulong ui64,
        long i64,
        nint nint,
        nuint nuint)
    {
        MethodAFloats(float.MaxValue, double.MaxValue);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static unsafe void MethodAFloats(
        float fl,
        double db)
    {
        var a = 1;
        var pointer = &a;
        MethodAPointer(pointer);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static unsafe void MethodAPointer(int* pointer)
    {
        MethodAOthers(
            string.Empty,
            new object(),
            new CustomClass(),
            default,
            [],
            [],
            new List<string>());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void MethodAOthers<T>(
        string s,
        object obj,
        CustomClass customClass,
        CustomStruct customStruct,
        CustomClass[] classArray,
        CustomStruct[] structArray,
        List<T> genericList)
    {
        Action(3);
        return;

        static void Action(int s) => InternalClassB<string, int>.DoubleInternalClassB.TripleInternalClassB<int>.MethodB(s, [3], TimeSpan.Zero, 0, ["a"], []);
    }

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
#if NET
    [DllImport("TestApplication.ContinuousProfiler.NativeDep")]
#else
    [DllImport("TestApplication.ContinuousProfiler.NativeDep.dll")]
#endif
    private static extern int OTelAutoCallbackTest(Callback fp, int n);

    internal static class InternalClassB<T1, T4>
    {
        internal static class DoubleInternalClassB
        {
            internal static class TripleInternalClassB<T3>
            {
                [MethodImpl(MethodImplOptions.NoInlining)]
                public static void MethodB<T2>(int testArg, T3[] a, T2 b, T4 t, IList<T1> c, IList<string> d)
                {
                    _ = OTelAutoCallbackTest(TestCallback, testArg);
                }
            }
        }
    }
}
