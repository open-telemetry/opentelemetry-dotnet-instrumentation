/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_RUNTIME_ASYNC_H_
#define OTEL_CLR_PROFILER_RUNTIME_ASYNC_H_

#include <corhlpr.h>

#include "clr_helpers.h"

namespace trace
{

// MethodImplAttributes.Async, introduced for the "runtime async" feature.
//
// It is deliberately not added to the vendored CoreCLR corhdr.h. Defining the constant here means
// a future refresh of that header cannot silently overwrite a local modification.
//
// A runtime-async method has no compiler-generated state machine; the runtime handles suspension.
// Its body does not return the declared Task: at `ret`, the evaluation stack is empty for Task or
// ValueTask, and contains the type argument for Task<T> or ValueTask<T>.
constexpr DWORD miAsync = 0x2000;

inline bool IsMiAsync(DWORD methodImplFlags)
{
    return (methodImplFlags & miAsync) != 0;
}

// Walks a declared return signature looking for Task-shaped syntax without resolving any names.
// On success, openTypeToken identifies the open return type and typeArg covers its single generic
// argument when isGenericInst is true.
bool ParseTaskLikeReturnShape(const TypeSignature& declared, mdToken& openTypeToken, bool& isGenericInst,
                              bool& isValueTypeShape, TypeSignature& typeArg);

// Maps the declared return type to what a runtime-async method body actually leaves on the stack:
// void for Task/ValueTask, and T for Task<T>/ValueTask<T>.
bool TryGetRuntimeAsyncEffectiveReturnType(const TypeSignature& declared,
                                           const ComPtr<IMetaDataImport2>& metadataImport, TypeSignature& effective);

} // namespace trace

#endif // OTEL_CLR_PROFILER_RUNTIME_ASYNC_H_
