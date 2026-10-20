// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_STACK_CAPTURE_TYPES_H_
#define OTEL_PROFILER_STACK_CAPTURE_TYPES_H_

#include <corhlpr.h>
#include <corprof.h>

namespace continuous_profiler
{

/// <summary>
/// Runtime type enumeration - shared between continuous_profiler and stack capture layer.
/// Determines suspension strategy: CLR-global (.NET Core) vs per-thread (.NET Framework).
/// </summary>
enum class RuntimeType
{
    Unknown,
    DotNetFramework,
    DotNetCore
};

/// <summary>
/// Per-frame data delivered to the caller during stack capture.
/// Bridge between continuous_profiler and IStackCapturer consumers.
/// Embedded inside the callback context to avoid copies.
/// </summary>
struct CapturedFrame
{
    FunctionID         functionId         = 0;
    UINT_PTR           instructionPointer = 0;
    COR_PRF_FRAME_INFO frameInfo          = 0;
    ULONG32            contextSize        = 0;
    BYTE*              context            = nullptr;
    ThreadID           threadId           = 0;
    bool               isUnmanagedFrame   = false;
};

} // namespace continuous_profiler

#endif // OTEL_PROFILER_STACK_CAPTURE_TYPES_H_