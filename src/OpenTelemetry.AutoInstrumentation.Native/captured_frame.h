// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_CAPTURED_FRAME_H_
#define OTEL_CAPTURED_FRAME_H_

#include <corhlpr.h>
#include <corprof.h>

namespace continuous_profiler
{

/// @brief Per-frame data delivered to the caller during stack capture.
/// This is the contract between IStackWalker and its consumers.
/// Embedded inside the strategy-level callback context to avoid copies.
struct CapturedFrame
{
    FunctionID         functionId         = 0;
    UINT_PTR           instructionPointer = 0;
    COR_PRF_FRAME_INFO frameInfo          = 0;
    ULONG32            contextSize        = 0;
    BYTE*              context            = nullptr;
    ThreadID           threadId           = 0;
    bool               isNativeWalkFrame  = false; // Set by RTL walker only
};

} // namespace continuous_profiler

#endif // OTEL_CAPTURED_FRAME_H_