// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_STACK_WALKER_H_
#define OTEL_PROFILER_STACK_WALKER_H_

#include <corhlpr.h>
#include <corprof.h>
#include <functional>
#include <unordered_set>
#include "string_utils.h"


namespace continuous_profiler
{
/// @brief Forward declaration of per-frame data contract delivered to the consumer during stack capture.
struct CapturedFrame;

/// @brief Callback invoked per frame during stack capture.
using FrameCallback = std::function<HRESULT(CapturedFrame* frame)>;

/// @brief Context passed to IStackWalker::CaptureStacks.
/// Replaces the void* parameter with a typed contract.
struct StackCaptureRequest
{
    FrameCallback onFrame;
};

/// @brief Minimal interface consumed by ContinuousProfiler.
/// Hides strategy selection, suspension model, native walk details,
/// and platform-specific concerns from the sampling loop.
class IStackWalker
{
public:
    virtual ~IStackWalker() = default;

    /// @brief Capture stacks for the given threads.
    /// Implementation owns all suspension/resume logic.
    virtual HRESULT CaptureStacks(const std::unordered_set<ThreadID>& threads,
                                  StackCaptureRequest*                request) = 0;

    /// @brief Resolve a native IP to a human-readable symbol name.
    virtual HRESULT ResolveNativeSymbolName(UINT_PTR        instructionPointer,
                                            trace::WSTRING& outName) = 0;
};

/// @brief Lifecycle events forwarded from CLR callbacks.
class IThreadLifecycleListener
{
public:
    virtual ~IThreadLifecycleListener() = default;
    virtual void OnThreadCreated(ThreadID threadId) {}
    virtual void OnThreadDestroyed(ThreadID threadId) {}
    virtual void OnThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) {}
    virtual void OnThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId) {}
};

} // namespace continuous_profiler

#endif // OTEL_PROFILER_STACK_WALKER_H_