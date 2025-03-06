// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace System;

internal static class OtlpSystemExtensions
{
    private const long NanosecondsPerTicks = 100;
    private const long UnixEpochTicks = 621355968000000000; // = DateTimeOffset.FromUnixTimeMilliseconds(0).Ticks

    public static ulong ToUnixTimeNanoseconds(this DateTime value)
        => (ulong)((value.Ticks - UnixEpochTicks) * NanosecondsPerTicks);
}
