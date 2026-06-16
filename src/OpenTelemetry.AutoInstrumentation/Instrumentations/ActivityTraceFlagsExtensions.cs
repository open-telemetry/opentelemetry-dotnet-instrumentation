// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations;

internal static class ActivityTraceFlagsExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string W3CFormatActivityTraceFlags(this ActivityTraceFlags flags)
    {
        // TODO NET11TODO fix casting 2 to ActivityTraceFlags when S.D.DS minimal version is bumped to 11
        return flags switch
        {
            ActivityTraceFlags.None => "00",
            ActivityTraceFlags.Recorded => "01",
            (ActivityTraceFlags)2 => "02",
            ActivityTraceFlags.Recorded | (ActivityTraceFlags)2 => "03",
            _ => Fallback((byte)flags),
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static string Fallback(byte flags)
        {
            Span<char> buffer = stackalloc char[2];

            buffer[0] = GetHexChar(flags >> 4);
            buffer[1] = GetHexChar(flags & 0xF);

            return buffer.ToString();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char GetHexChar(int value)
    {
        return (char)(value + (value < 10 ? '0' : 'a' - 10));
    }
}
