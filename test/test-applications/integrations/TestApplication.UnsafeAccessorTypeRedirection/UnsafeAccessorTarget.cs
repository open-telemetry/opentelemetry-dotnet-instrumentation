// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

internal static class UnsafeAccessorTarget
{
    // ServiceProviderOptions has a public parameterless constructor and ValidateScopes property that are stable parts of
    // the dependency-injection implementation. The constructor's return value and the getter's receiver cover both places
    // where UnsafeAccessorTypeAttribute supplies the concrete type to the runtime.
#if COMPLETE_UNSAFE_ACCESSOR
    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
#endif
    [return: UnsafeAccessorType(UnsafeAccessorBuildSettings.TypeName)]
    internal static extern object NewServiceProviderOptions();

#if COMPLETE_UNSAFE_ACCESSOR
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_ValidateScopes")]
#endif
    internal static extern bool GetValidateScopes(
        [UnsafeAccessorType(UnsafeAccessorBuildSettings.TypeName)] object value);
}
