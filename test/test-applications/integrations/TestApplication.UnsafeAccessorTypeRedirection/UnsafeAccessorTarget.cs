// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

internal static class UnsafeAccessorTarget
{
    // ServiceCollection has a public parameterless constructor and Count property that are stable parts of the
    // dependency-injection abstractions. The constructor's return value and the getter's receiver cover both places
    // where UnsafeAccessorTypeAttribute supplies the concrete type to the runtime.
#if COMPLETE_UNSAFE_ACCESSOR
    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
#endif
    [return: UnsafeAccessorType(UnsafeAccessorBuildSettings.TypeName)]
    internal static extern object NewServiceCollection();

#if COMPLETE_UNSAFE_ACCESSOR
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Count")]
#endif
    internal static extern int GetCount(
        [UnsafeAccessorType(UnsafeAccessorBuildSettings.TypeName)] object value);
}
