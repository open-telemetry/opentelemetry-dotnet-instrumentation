// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_NATIVE_SYMBOL_RESOLVER_H_
#define OTEL_NATIVE_SYMBOL_RESOLVER_H_

#include "string_utils.h"

namespace ProfilerStackCapture
{

/// @brief Platform-agnostic interface for resolving native instruction
///        pointers to human-readable symbol names.
class INativeSymbolResolver
{
public:
    virtual ~INativeSymbolResolver() = default;

    /// @brief Resolve a native IP to a symbol name (e.g. "ntdll!RtlUserThreadStart").
    /// @return true if resolved, false if unknown.
    virtual bool Resolve(UINT_PTR ip, trace::WSTRING& outName) = 0;
};

} // namespace ProfilerStackCapture

#endif // OTEL_NATIVE_SYMBOL_RESOLVER_H_