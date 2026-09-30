// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation;

internal static class NativeCallTargetUnmanagedMemoryHelper
{
    private const int SizeOfMemorySegment = 750 * 1024;
    private static readonly int SizeOfPointer = Marshal.SizeOf<IntPtr>();
    private static readonly List<IntPtr> Segments = new(5);
    private static IntPtr _currentSegment;
    private static int _segmentOffset;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntPtr Allocate(int bytesCount)
    {
        if (_currentSegment == IntPtr.Zero)
        {
            CreateSegment();
        }

        var offset = _segmentOffset;
        if (SizeOfMemorySegment - offset < bytesCount)
        {
            if (bytesCount > SizeOfMemorySegment)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(bytesCount), "The number of bytes is bigger than the maximum size of a memory segment.");
            }

            CreateSegment();
            offset = 0;
        }

        _segmentOffset = offset + bytesCount;
        return _currentSegment + offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Free()
    {
        for (var i = 0; i < Segments.Count; i++)
        {
            Marshal.FreeCoTaskMem(Segments[i]);
        }

        _currentSegment = IntPtr.Zero;
        _segmentOffset = 0;
        Segments.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe IntPtr AllocateAndWriteUtf16String(string? value)
    {
        if (value is null)
        {
            return IntPtr.Zero;
        }

        var stringPtrSize = value.Length * sizeof(char);
        var stringPtr = Allocate(stringPtrSize + sizeof(char));
        fixed (char* source = value)
        {
            Buffer.MemoryCopy(source, (void*)stringPtr, stringPtrSize, stringPtrSize);
            ((char*)stringPtr)[value.Length] = '\0';
        }

        return stringPtr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntPtr AllocateAndWriteUtf16StringArray(string[]? array)
    {
        if (array is null || array.Length == 0)
        {
            return IntPtr.Zero;
        }

        var unmanagedArray = Allocate(array.Length * SizeOfPointer);
        for (var i = 0; i < array.Length; i++)
        {
            Marshal.WriteIntPtr(unmanagedArray, i * SizeOfPointer, AllocateAndWriteUtf16String(array[i]));
        }

        return unmanagedArray;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntPtr AllocateAndWriteUtf16StringArray(string arrayItem1)
    {
        var unmanagedArray = Allocate(SizeOfPointer);
        Marshal.WriteIntPtr(unmanagedArray, 0, AllocateAndWriteUtf16String(arrayItem1));
        return unmanagedArray;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntPtr AllocateAndWriteUtf16StringArray(string arrayItem1, string arrayItem2)
    {
        var unmanagedArray = Allocate(2 * SizeOfPointer);
        Marshal.WriteIntPtr(unmanagedArray, 0, AllocateAndWriteUtf16String(arrayItem1));
        Marshal.WriteIntPtr(unmanagedArray, SizeOfPointer, AllocateAndWriteUtf16String(arrayItem2));
        return unmanagedArray;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntPtr AllocateAndWriteUtf16StringArray(string arrayItem1, string arrayItem2, string arrayItem3)
    {
        var unmanagedArray = Allocate(3 * SizeOfPointer);
        Marshal.WriteIntPtr(unmanagedArray, 0, AllocateAndWriteUtf16String(arrayItem1));
        Marshal.WriteIntPtr(unmanagedArray, SizeOfPointer, AllocateAndWriteUtf16String(arrayItem2));
        Marshal.WriteIntPtr(unmanagedArray, 2 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem3));
        return unmanagedArray;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntPtr AllocateAndWriteUtf16StringArray(string arrayItem1, string arrayItem2, string arrayItem3, string arrayItem4)
    {
        var unmanagedArray = Allocate(4 * SizeOfPointer);
        Marshal.WriteIntPtr(unmanagedArray, 0, AllocateAndWriteUtf16String(arrayItem1));
        Marshal.WriteIntPtr(unmanagedArray, SizeOfPointer, AllocateAndWriteUtf16String(arrayItem2));
        Marshal.WriteIntPtr(unmanagedArray, 2 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem3));
        Marshal.WriteIntPtr(unmanagedArray, 3 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem4));
        return unmanagedArray;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntPtr AllocateAndWriteUtf16StringArray(string arrayItem1, string arrayItem2, string arrayItem3, string arrayItem4, string arrayItem5)
    {
        var unmanagedArray = Allocate(5 * SizeOfPointer);
        Marshal.WriteIntPtr(unmanagedArray, 0, AllocateAndWriteUtf16String(arrayItem1));
        Marshal.WriteIntPtr(unmanagedArray, SizeOfPointer, AllocateAndWriteUtf16String(arrayItem2));
        Marshal.WriteIntPtr(unmanagedArray, 2 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem3));
        Marshal.WriteIntPtr(unmanagedArray, 3 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem4));
        Marshal.WriteIntPtr(unmanagedArray, 4 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem5));
        return unmanagedArray;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntPtr AllocateAndWriteUtf16StringArray(string arrayItem1, string arrayItem2, string arrayItem3, string arrayItem4, string arrayItem5, string arrayItem6)
    {
        var unmanagedArray = Allocate(6 * SizeOfPointer);
        Marshal.WriteIntPtr(unmanagedArray, 0, AllocateAndWriteUtf16String(arrayItem1));
        Marshal.WriteIntPtr(unmanagedArray, SizeOfPointer, AllocateAndWriteUtf16String(arrayItem2));
        Marshal.WriteIntPtr(unmanagedArray, 2 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem3));
        Marshal.WriteIntPtr(unmanagedArray, 3 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem4));
        Marshal.WriteIntPtr(unmanagedArray, 4 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem5));
        Marshal.WriteIntPtr(unmanagedArray, 5 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem6));
        return unmanagedArray;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntPtr AllocateAndWriteUtf16StringArray(string arrayItem1, string arrayItem2, string arrayItem3, string arrayItem4, string arrayItem5, string arrayItem6, string arrayItem7)
    {
        var unmanagedArray = Allocate(7 * SizeOfPointer);
        Marshal.WriteIntPtr(unmanagedArray, 0, AllocateAndWriteUtf16String(arrayItem1));
        Marshal.WriteIntPtr(unmanagedArray, SizeOfPointer, AllocateAndWriteUtf16String(arrayItem2));
        Marshal.WriteIntPtr(unmanagedArray, 2 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem3));
        Marshal.WriteIntPtr(unmanagedArray, 3 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem4));
        Marshal.WriteIntPtr(unmanagedArray, 4 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem5));
        Marshal.WriteIntPtr(unmanagedArray, 5 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem6));
        Marshal.WriteIntPtr(unmanagedArray, 6 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem7));
        return unmanagedArray;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntPtr AllocateAndWriteUtf16StringArray(string arrayItem1, string arrayItem2, string arrayItem3, string arrayItem4, string arrayItem5, string arrayItem6, string arrayItem7, string arrayItem8)
    {
        var unmanagedArray = Allocate(8 * SizeOfPointer);
        Marshal.WriteIntPtr(unmanagedArray, 0, AllocateAndWriteUtf16String(arrayItem1));
        Marshal.WriteIntPtr(unmanagedArray, SizeOfPointer, AllocateAndWriteUtf16String(arrayItem2));
        Marshal.WriteIntPtr(unmanagedArray, 2 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem3));
        Marshal.WriteIntPtr(unmanagedArray, 3 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem4));
        Marshal.WriteIntPtr(unmanagedArray, 4 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem5));
        Marshal.WriteIntPtr(unmanagedArray, 5 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem6));
        Marshal.WriteIntPtr(unmanagedArray, 6 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem7));
        Marshal.WriteIntPtr(unmanagedArray, 7 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem8));
        return unmanagedArray;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntPtr AllocateAndWriteUtf16StringArray(string arrayItem1, string arrayItem2, string arrayItem3, string arrayItem4, string arrayItem5, string arrayItem6, string arrayItem7, string arrayItem8, string arrayItem9)
    {
        var unmanagedArray = Allocate(9 * SizeOfPointer);
        Marshal.WriteIntPtr(unmanagedArray, 0, AllocateAndWriteUtf16String(arrayItem1));
        Marshal.WriteIntPtr(unmanagedArray, SizeOfPointer, AllocateAndWriteUtf16String(arrayItem2));
        Marshal.WriteIntPtr(unmanagedArray, 2 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem3));
        Marshal.WriteIntPtr(unmanagedArray, 3 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem4));
        Marshal.WriteIntPtr(unmanagedArray, 4 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem5));
        Marshal.WriteIntPtr(unmanagedArray, 5 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem6));
        Marshal.WriteIntPtr(unmanagedArray, 6 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem7));
        Marshal.WriteIntPtr(unmanagedArray, 7 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem8));
        Marshal.WriteIntPtr(unmanagedArray, 8 * SizeOfPointer, AllocateAndWriteUtf16String(arrayItem9));
        return unmanagedArray;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateSegment()
    {
        _currentSegment = Marshal.AllocCoTaskMem(SizeOfMemorySegment);
        _segmentOffset = 0;
        Segments.Add(_currentSegment);
    }
}
