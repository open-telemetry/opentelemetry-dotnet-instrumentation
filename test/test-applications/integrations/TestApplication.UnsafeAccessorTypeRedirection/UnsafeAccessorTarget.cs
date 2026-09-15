// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

internal static class UnsafeAccessorTarget
{
#if COMPLETE_UNSAFE_ACCESSOR
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "Unused")]
#endif
    [return: UnsafeAccessorType(UnsafeAccessorBuildSettings.TypeName)]
    internal static extern object Unused(
        [UnsafeAccessorType(UnsafeAccessorBuildSettings.TypeName)] object value);
}
